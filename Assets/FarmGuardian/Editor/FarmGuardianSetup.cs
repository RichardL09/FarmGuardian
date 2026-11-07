using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace FarmGuardian.Editor
{
    public static class FarmGuardianSetup
    {
        const string Root = "Assets/FarmGuardian";
        const string Source = "Assets/UTS_Project/";
        public const string ScenePath = Root + "/Scenes/FarmGuardian.unity";

        [MenuItem("Tools/Farm Guardian/Build Game")]
        public static void BuildGame()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string folder in new[] { "Scenes", "Data", "Materials", "Animation", "Prefabs" })
                Directory.CreateDirectory(Root + "/" + folder);
            AssetDatabase.Refresh();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit shader is unavailable.");
            var config = AssetDatabase.LoadAssetAtPath<FarmConfig>(Root + "/Data/FarmGuardianConfig.asset");
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<FarmConfig>();
                AssetDatabase.CreateAsset(config, Root + "/Data/FarmGuardianConfig.asset");
            }
            config.farmerModel = Required<GameObject>(Source + "farmer/source/Player_SkinFarmer.obj");
            config.thiefModel = Required<GameObject>(Source + "burglar/source/Player_SkinBurglar.obj");
            config.townModel = Required<GameObject>(Source + "village-town-assets/source/PictureCrossTownsAssets.fbx");
            config.farmerMaterial = Material("Farmer", Source + "farmer/textures/Design_sem_nome_-_2026-02-22T080022.481.png");
            config.thiefMaterial = Material("Burglar", Source + "burglar/textures/Design_sem_nome_-_2026-02-12T102748.810.png");
            config.animalMaterial = Material("Animals", Source + "kenney_cube-pets_1.0/Models/FBX format/Textures/colormap.png");
            config.environmentMaterial = Material("Town", Source + "village-town-assets/textures/Gradients.png");
            config.farmerModel = FarmCharacterRigBuilder.Create("Farmer", Source + "farmer/source/Player_SkinFarmer.obj", config.farmerMaterial);
            config.thiefModel = FarmCharacterRigBuilder.Create("Burglar", Source + "burglar/source/Player_SkinBurglar.obj", config.thiefMaterial);
            string[] animals = { "cow", "pig", "chick", "bunny" };
            config.animalModels = new GameObject[animals.Length];
            config.animalAnimators = new RuntimeAnimatorController[animals.Length];
            for (int i = 0; i < animals.Length; i++)
            {
                string path = Source + "kenney_cube-pets_1.0/Models/FBX format/animal-" + animals[i] + ".fbx";
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.importAnimation = true;
                var clips = importer.defaultClipAnimations;
                foreach (var clip in clips) clip.loopTime = clip.name != "static";
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
                config.animalModels[i] = Required<GameObject>(path);
                config.animalAnimators[i] = AnimalAnimator(animals[i], path);
            }
            EditorUtility.SetDirty(config);
            FarmMenuArtSetup.Apply(config);
            if(Resources.Load<FarmUiTheme>("UI/FarmUiTheme")==null) FarmUiSetup.Build();

            // A saved bootstrap prefab owns all runtime systems. Source assets remain separate.
            var temporary = new GameObject("Farm Guardian");
            temporary.AddComponent<FarmSession>().config = config;
            var bootstrap = PrefabUtility.SaveAsPrefabAsset(temporary, Root + "/Prefabs/FarmGuardian.prefab");
            UnityEngine.Object.DestroyImmediate(temporary);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            PrefabUtility.InstantiatePrefab(bootstrap);
            var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>(); cameraObject.AddComponent<AudioListener>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.20f, 0.19f);
            camera.transform.position = new Vector3(0, 22, -24); camera.transform.LookAt(Vector3.zero);
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            var lightObject = new GameObject("Farm Sun");
            var sun = lightObject.AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.35f;
            sun.color = new Color(1, 0.95f, 0.83f); sun.shadows = LightShadows.None;
            lightObject.transform.rotation = Quaternion.Euler(48, -32, 0);
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.73f, 0.79f, 0.76f);
            RenderSettings.fog = false;
            EditorSceneManager.SaveScene(scene, ScenePath);

            var existing = EditorBuildSettings.scenes.Where(s => s.path != ScenePath)
                .Select(s => new EditorBuildSettingsScene(s.path, false)).ToList();
            existing.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = existing.ToArray();
            PlayerSettings.productName = "Farm Guardian";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.defaultScreenWidth = 1600; PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;PlayerSettings.defaultIsNativeResolution=true;
            PlayerSettings.runInBackground = false;
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var obstacleLayer = tags.FindProperty("layers").GetArrayElementAtIndex(FarmWorld.SolidLayer);
            if (string.IsNullOrEmpty(obstacleLayer.stringValue)) obstacleLayer.stringValue = "FarmObstacles";
            tags.ApplyModifiedPropertiesWithoutUndo();
            QualitySettings.SetQualityLevel(0, true);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log("FARM_GUARDIAN_BUILD_OK: " + ScenePath + ". Enter Play Mode for the main menu. Runtime maps assemble from verified source models.");
        }

        static T Required<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException("Required verified asset could not be loaded: " + path);
            return asset;
        }

        static Material Material(string name, string texturePath)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.mainTexture = Required<Texture2D>(texturePath);
            material.color = Color.white; material.SetFloat("_Smoothness", 0.05f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static RuntimeAnimatorController AnimalAnimator(string animal, string modelPath)
        {
            string path = Root + "/Animation/" + animal + ".controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller != null) return controller;
            var clips = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToArray();
            var idle = clips.FirstOrDefault(c => c.name == "idle" || c.name.EndsWith("|idle"));
            var walk = clips.FirstOrDefault(c => c.name == "walk" || c.name.EndsWith("|walk"));
            if (idle == null || walk == null)
                throw new InvalidOperationException("Missing idle/walk clips in " + modelPath + "; clips: " + string.Join(",", clips.Select(c => c.name)));
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
            var machine = controller.layers[0].stateMachine;
            var idleState = machine.AddState("Idle"); idleState.motion = idle;
            var walkState = machine.AddState("Walk"); walkState.motion = walk;
            machine.defaultState = idleState;
            var start = idleState.AddTransition(walkState); start.hasExitTime = false; start.duration = 0.12f;
            start.AddCondition(AnimatorConditionMode.If, 0, "Moving");
            var stop = walkState.AddTransition(idleState); stop.hasExitTime = false; stop.duration = 0.12f;
            stop.AddCondition(AnimatorConditionMode.IfNot, 0, "Moving");
            return controller;
        }

        [MenuItem("Tools/Farm Guardian/Validate Configuration")]
        public static void Validate()
        {
            var config = Required<FarmConfig>(Root + "/Data/FarmGuardianConfig.asset");
            for (int level = 1; level <= 300; level++)
            {
                var spec = config.GetLevel(level);
                if (spec.animalCount < 3 || spec.animalCount > 8 || spec.plantingCount < 0 || spec.plantingCount > 3 ||
                    spec.typeCount < 1 || spec.typeCount > config.animalModels.Length || spec.seconds < 60 || spec.thiefSpeed > config.thiefMaxSpeed)
                    throw new InvalidOperationException("Invalid mission at level " + level);
            }
            if (config.GetLevel(1).theme != 0 || config.GetLevel(16).theme != 1 || config.GetLevel(31).theme != 2 ||
                config.GetLevel(46).theme != 3 || config.GetLevel(61).theme != 0)
                throw new InvalidOperationException("Theme rotation failed.");
            if (config.GetLevel(2).plantingCount != 1) throw new InvalidOperationException("Level 2 planting objective missing.");
            Debug.Log("FARM_GUARDIAN_CONFIG_OK: 300 level configurations and theme boundaries validated.");
        }

        public static void BuildAndValidate() { BuildGame(); Validate(); }
    }
}
