using System.IO;
using UnityEditor;
using UnityEngine;

namespace FarmGuardian.Editor
{
    public static class FarmCharacterRigBuilder
    {
        const string Root = "Assets/FarmGuardian";

        [MenuItem("Tools/Farm Guardian/Update Character Rigs")]
        public static void UpdateRigs()
        {
            var config = AssetDatabase.LoadAssetAtPath<FarmConfig>(Root + "/Data/FarmGuardianConfig.asset");
            if (config == null) { Debug.LogError("Run Build Game first: configuration is missing."); return; }
            config.farmerModel = Create("Farmer", "Assets/UTS_Project/farmer/source/Player_SkinFarmer.obj", config.farmerMaterial);
            config.thiefModel = Create("Burglar", "Assets/UTS_Project/burglar/source/Player_SkinBurglar.obj", config.thiefMaterial);
            EditorUtility.SetDirty(config); AssetDatabase.SaveAssets();
            Debug.Log("FARM_GUARDIAN_RIGS_OK: Farmer and Burglar walking rigs generated. Source OBJs preserved.");
        }

        public static GameObject Create(string name, string sourcePath, Material material,string outputFolder=null,float sourceYaw=0)
        {
            Directory.CreateDirectory(Root + "/Meshes");
            Directory.CreateDirectory(Root + "/Prefabs/Characters");
            AssetDatabase.Refresh();
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null) throw new System.InvalidOperationException("Missing inspected character: " + sourcePath);
            GameObject instance = Object.Instantiate(source);
            instance.transform.rotation=Quaternion.Euler(0,sourceYaw,0)*instance.transform.rotation;
            var root = new GameObject(name + " Walking Rig");
            try
            {
                instance.transform.position = Vector3.zero;
                var renderers = instance.GetComponentsInChildren<MeshRenderer>();
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                float height = 1.8f;
                float scale = height / bounds.size.y;
                Matrix4x4 normalize = Matrix4x4.TRS(new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z) * scale,
                    Quaternion.identity, Vector3.one * scale);
                var animation = root.AddComponent<FarmCharacterAnimation>();
                Transform body = Bone("Body", root.transform, new Vector3(0, height * 0.40f, 0));
                var leftLeg = Bone("Left thigh", body, new Vector3(-height * 0.10f, height * 0.37f, 0));
                var rightLeg = Bone("Right thigh", body, new Vector3(height * 0.10f, height * 0.37f, 0));
                var leftKnee = Bone("Left shin", leftLeg, new Vector3(-height * 0.10f, height * 0.16f, 0));
                var rightKnee = Bone("Right shin", rightLeg, new Vector3(height * 0.10f, height * 0.16f, 0));
                var leftArm = Bone("Left shoulder", body, new Vector3(-height * 0.15f, height * 0.53f, 0));
                var rightArm = Bone("Right shoulder", body, new Vector3(height * 0.15f, height * 0.53f, 0));
                Transform[] bones = { body, leftLeg, rightLeg, leftKnee, rightKnee, leftArm, rightArm };
                animation.body = body; animation.leftLeg = leftLeg; animation.rightLeg = rightLeg;
                animation.leftKnee = leftKnee; animation.rightKnee = rightKnee;
                animation.leftArm = leftArm; animation.rightArm = rightArm;
                int index = 0;
                foreach (var filter in instance.GetComponentsInChildren<MeshFilter>())
                {
                    if (filter.sharedMesh == null) continue;
                    var mesh = Object.Instantiate(filter.sharedMesh);
                    mesh.name = name + " skinned mesh " + index;
                    var points = mesh.vertices;
                    var normals = mesh.normals;
                    Matrix4x4 toRig = normalize * filter.transform.localToWorldMatrix;
                    var weights = new BoneWeight[points.Length];
                    for (int i = 0; i < points.Length; i++)
                    {
                        points[i] = toRig.MultiplyPoint3x4(points[i]);
                        if (normals.Length == points.Length) normals[i] = toRig.MultiplyVector(normals[i]).normalized;
                        weights[i] = Weights(points[i], height);
                    }
                    mesh.vertices = points;
                    if (toRig.determinant < 0)
                        for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                        {
                            int[] triangles = mesh.GetTriangles(submesh);
                            for (int i = 0; i < triangles.Length; i += 3)
                            { int swap = triangles[i + 1]; triangles[i + 1] = triangles[i + 2]; triangles[i + 2] = swap; }
                            mesh.SetTriangles(triangles, submesh);
                        }
                    if (normals.Length == points.Length) mesh.normals = normals; else mesh.RecalculateNormals();
                    mesh.boneWeights = weights;
                    var binds = new Matrix4x4[bones.Length];
                    for (int i = 0; i < bones.Length; i++) binds[i] = bones[i].worldToLocalMatrix * root.transform.localToWorldMatrix;
                    mesh.bindposes = binds; mesh.RecalculateBounds(); mesh.RecalculateTangents();
                    string meshPath = Root + "/Meshes/" + name + index++ + ".asset";
                    var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if (existing == null) AssetDatabase.CreateAsset(mesh, meshPath);
                    else { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = existing; }
                    var part = new GameObject(filter.name); part.transform.SetParent(root.transform, false);
                    var skin = part.AddComponent<SkinnedMeshRenderer>();
                    skin.sharedMesh = mesh; skin.bones = bones; skin.rootBone = body;
                    skin.localBounds = new Bounds(Vector3.up * 0.9f, new Vector3(2.5f, 2.6f, 2.5f));
                    var materials = new Material[mesh.subMeshCount];
                    for (int i = 0; i < materials.Length; i++) materials[i] = material;
                    skin.sharedMaterials = materials;
                }
                FarmCharacterControllerBuilder.Attach(root);
                return PrefabUtility.SaveAsPrefabAsset(root, (outputFolder??Root + "/Prefabs/Characters") + "/" + name + ".prefab");
            }
            finally { Object.DestroyImmediate(instance); Object.DestroyImmediate(root); }
        }

        static Transform Bone(string name, Transform parent, Vector3 position)
        {
            var bone = new GameObject(name).transform; bone.SetParent(parent, false); bone.position = position; return bone;
        }

        static BoneWeight Weights(Vector3 point, float height)
        {
            float y = point.y / height, x = Mathf.Abs(point.x) / height;
            // Thresholds fitted to the inspected source OBJs, which share the same stylized anatomy.
            float arm = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.185f, 0.26f, x)) *
                (1 - Blend(0.50f, 0.57f, y)) * Blend(0.20f, 0.28f, y);
            if (arm > 0.01f)
                return new BoneWeight { boneIndex0 = point.x < 0 ? 5 : 6, weight0 = arm, boneIndex1 = 0, weight1 = 1 - arm };
            float leg = 1 - Blend(0.28f, 0.40f, y);
            float knee = (1 - Blend(0.12f, 0.23f, y)) * leg;
            int thigh = point.x < 0 ? 1 : 2;
            return new BoneWeight { boneIndex0 = thigh, weight0 = leg - knee, boneIndex1 = point.x < 0 ? 3 : 4,
                weight1 = knee, boneIndex2 = 0, weight2 = 1 - leg };
        }

        static float Blend(float low, float high, float value) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(low, high, value));
    }
}
