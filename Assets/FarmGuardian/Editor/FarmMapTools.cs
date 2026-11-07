using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FarmGuardian.Editor
{
    [InitializeOnLoad]
    public static class FarmMapTools
    {
        static FarmMapTools()
        {
            EditorApplication.playModeStateChanged += state =>
            { if (state == PlayModeStateChange.ExitingEditMode) ClosePreviews(); };
        }

        static void ClosePreviews()
        {
            for (int i = SceneManager.sceneCount-1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.name.StartsWith("Map preview - ") || !string.IsNullOrEmpty(scene.path)) continue;
                foreach (var root in scene.GetRootGameObjects())
                    if (root != null && root.name == "Map preview systems" && root.TryGetComponent<FarmWorld>(out var world)) world.Clear();
                EditorSceneManager.CloseScene(scene,true);
            }
        }
        public static readonly string[] Slugs = { "Countryside", "Woodland", "Mountain", "Advanced" };
        const string DirectoryPath = "Assets/FarmGuardian/Docs/Maps";
        static string F(float value) => value.ToString("0.##",CultureInfo.InvariantCulture);
        static string X(float value) => F(56 + (value + 22) * 18);
        static string Y(float value) => F(104 + (18 - value) * 18);
        static string Safe(string value) => System.Security.SecurityElement.Escape(value);

        [MenuItem("Tools/Farm Guardian/Maps/Export Layout Diagrams")]
        public static void ExportDiagrams()
        {
            Directory.CreateDirectory(DirectoryPath);
            for (int theme = 0; theme < FarmMapLayout.ThemeCount; theme++)
                File.WriteAllText(DirectoryPath + "/" + Slugs[theme] + ".svg",Diagram(FarmMapLayout.ForTheme(theme)));
            AssetDatabase.Refresh();
            Debug.Log("FARM_GUARDIAN_MAP_DIAGRAMS_OK: " + DirectoryPath);
        }

        // Batch entry point also verifies that the edit-time assembly workflow is usable.
        public static void ValidateAndExport()
        {
            if (Application.isBatchMode) EditorSceneManager.OpenScene(FarmGuardianSetup.ScenePath,OpenSceneMode.Single);
            FarmGuardianSetup.Validate();
            ExportDiagrams();
            try
            {
                for (int theme = 0; theme < FarmMapLayout.ThemeCount; theme++)
                {
                    Preview(theme);
                    var scene = SceneManager.GetActiveScene();
                    FarmWorld world = null;
                    foreach (var root in scene.GetRootGameObjects())
                        if (root.TryGetComponent<FarmWorld>(out var found)) world = found;
                    if (world == null || world.Layout.Theme != theme)
                        throw new System.InvalidOperationException("Preview scene construction failed.");
                    foreach (string source in new[] { "Barn", "Windmill", "Well", "Hay_Bale", "Hay_Cart", "Tree_Square", "Tree_Pine", "Boulder" })
                        if (world.FindTownProp(source) == null)
                            throw new System.InvalidOperationException("Required town source model missing: " + source);
                }
            }
            finally { ClosePreviews(); }
            Debug.Log("FARM_GUARDIAN_MAP_PREVIEWS_OK: four edit-time previews, all objective slots and source prop references validated.");
        }

        public static string Diagram(FarmMapLayout map)
        {
            var svg = new StringBuilder();
            svg.Append("<svg xmlns='http://www.w3.org/2000/svg' width='960' height='1000' viewBox='0 0 960 1000'><rect width='960' height='1000' fill='#f5f0e2'/><style>text{font-family:Arial,sans-serif;fill:#203a30} .minor{font-size:12px} .label{font-size:14px;font-weight:700;paint-order:stroke;stroke:#fff6df;stroke-width:3px;stroke-linejoin:round}</style>");
            svg.Append("<text x='56' y='45' font-size='28' font-weight='700'>" + Safe(map.Name) + "</text><text x='56' y='76' font-size='16'>Levels " + (map.Theme*15+1) + "–" + (map.Theme*15+15) + " • north ↑ • X/Z in metres • all potential objective slots</text>");
            svg.Append("<rect x='56' y='104' width='792' height='648' rx='10' fill='" + (map.Theme == 1 ? "#aac49b" : map.Theme == 2 ? "#bdc2a5" : "#c9d7a0") + "'/>");
            for (int i = -20; i <= 20; i += 5)
            { svg.Append("<path d='M"+X(i)+" 104V752' stroke='#ffffff' opacity='.3'/><text x='"+X(i)+"' y='780' class='minor' text-anchor='middle'>"+i+"</text>"); }
            for (int i = -15; i <= 15; i += 5)
            { svg.Append("<path d='M56 "+Y(i)+"H848' stroke='#ffffff' opacity='.3'/><text x='37' y='"+Y(i)+"' class='minor' text-anchor='middle'>"+i+"</text>"); }
            foreach (var route in map.Routes)
            {
                svg.Append("<polyline fill='none' stroke='"+(route.Alternative ? "#e9c881" : "#cda260")+"' stroke-width='"+F(route.Width*18)+"' stroke-linejoin='round' stroke-linecap='round' points='");
                foreach (var point in route.Nodes) svg.Append(X(point.x)+","+Y(point.z)+" ");
                svg.Append("'/>");
                if (route.Alternative)
                {
                    svg.Append("<polyline fill='none' stroke='#8a6b3c' stroke-width='1.8' stroke-dasharray='7 6' points='");
                    foreach (var point in route.Nodes) svg.Append(X(point.x)+","+Y(point.z)+" ");
                    svg.Append("'/>");
                }
            }
            foreach (var obstacle in map.Obstacles)
            {
                string color = obstacle.Asset == "Grove" ? "#456e42" : obstacle.Asset == "Ridge" || obstacle.Asset == "Boulder" ? "#7a8585" : obstacle.Asset == "Barn" ? "#aa5841" : "#8c6945";
                Rect(svg,obstacle.Position,obstacle.Size.x,obstacle.Size.z,color);
                if (obstacle.Size.x > 2 && obstacle.Size.z > 2) Label(svg,obstacle.Position,obstacle.Asset.ToUpperInvariant());
            }
            foreach (var centre in map.CrossingCentres) { Rect(svg,centre,7,3.4f,"#d2a778"); Label(svg,centre,"PASS"); }
            for (int i = 0; i < map.Pens.Length; i++)
            {
                Vector3 point = map.Pens[i]; Rect(svg,point,3.6f,4.5f,"#f2dda4");
                svg.Append("<path d='M"+X(point.x-1.8f)+" "+Y(point.z-2.25f)+"V"+Y(point.z+2.25f)+"H"+X(point.x+1.8f)+"V"+Y(point.z-2.25f)+"' fill='none' stroke='#71472a' stroke-width='4'/>");
                Label(svg,point,new[] { "COW", "PIG", "CHICK", "BUNNY" }[i]);
                Dot(svg,point+Vector3.back*2.6f,"▼","#f8f3df",9);
            }
            for (int i = 0; i < map.Plots.Length; i++)
            { Rect(svg,map.Plots[i],2.3f,1.8f,"#825b3a"); Label(svg,map.Plots[i],"F"+(i+1)); }
            for (int i = 0; i < map.ThiefEntrances.Length; i++) Dot(svg,map.ThiefEntrances[i],"T","#dbabcb",8);
            for (int i = 0; i < map.AnimalSpawns.Length; i++) Dot(svg,map.AnimalSpawns[i],"A"+(i+1),"#fff9e6",14);
            Dot(svg,map.PlayerSpawn,"P","#62b8d1",17); Dot(svg,map.Escape,"E","#e17c61",17); Dot(svg,map.ResponseHub,"H","#89c5b2",15);
            svg.Append("<rect x='"+X(-21)+"' y='"+Y(17)+"' width='756' height='612' fill='none' stroke='#6f5339' stroke-width='5'/>");
            svg.Append("<text x='56' y='821' font-size='17' font-weight='700'>P player   E thief exit   H response hub   A animal   F planting   T thief entrance</text>");
            svg.Append("<text x='56' y='851' font-size='15'>Brown: main routes   Dashed: alternatives   Red: barns   Green: grove   Grey: rock</text><text x='56' y='879' font-size='15'>Pen arrows show open south entrances. Slots activate with mission difficulty.</text><text x='56' y='907' font-size='15'>Route widths: 2.8–3.6 m; mountain openings: 4.4 m; perimeter fence: 42 × 34 m.</text><text x='56' y='951' font-size='13'>Source of truth: FarmMapLayout.cs • See MAP_DESIGN.md for hierarchy, collision and build instructions.</text></svg>");
            return svg.ToString();
        }

        static void Rect(StringBuilder svg,Vector3 centre,float width,float depth,string fill)
        { svg.Append("<rect x='"+X(centre.x-width*.5f)+"' y='"+Y(centre.z+depth*.5f)+"' width='"+F(width*18)+"' height='"+F(depth*18)+"' fill='"+fill+"' rx='3'/>"); }
        static void Label(StringBuilder svg,Vector3 point,string text)
        { svg.Append("<text x='"+X(point.x)+"' y='"+Y(point.z)+"' dominant-baseline='central' text-anchor='middle' class='label'>"+Safe(text)+"</text>"); }
        static void Dot(StringBuilder svg,Vector3 point,string text,string fill,float radius)
        { svg.Append("<circle cx='"+X(point.x)+"' cy='"+Y(point.z)+"' r='"+F(radius)+"' fill='"+fill+"' stroke='#3a4c3a' stroke-width='1.5'/>"); Label(svg,point,text); }

        [MenuItem("Tools/Farm Guardian/Maps/Preview/Countryside")]
        public static void PreviewCountryside() => Preview(0);
        [MenuItem("Tools/Farm Guardian/Maps/Preview/Woodland")]
        public static void PreviewWoodland() => Preview(1);
        [MenuItem("Tools/Farm Guardian/Maps/Preview/Mountain")]
        public static void PreviewMountain() => Preview(2);
        [MenuItem("Tools/Farm Guardian/Maps/Preview/Advanced")]
        public static void PreviewAdvanced() => Preview(3);

        static void Preview(int theme)
        {
            if (Application.isPlaying) { Debug.LogWarning("Exit Play Mode before building an edit-time preview."); return; }
            ClosePreviews();
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                { Debug.LogWarning("Open the saved FarmGuardian scene before creating a map preview; unsaved scenes are preserved."); return; }
            var config = AssetDatabase.LoadAssetAtPath<FarmConfig>("Assets/FarmGuardian/Data/FarmGuardianConfig.asset");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            scene.name = "Map preview - " + Slugs[theme]; SceneManager.SetActiveScene(scene);
            var owner = new GameObject("Map preview systems");
            var session = owner.AddComponent<FarmSession>(); session.enabled = false; session.config = config;
            var world = owner.AddComponent<FarmWorld>(); world.Session = session;
            var spec = config.GetLevel(theme * 15 + 15); spec.theme = theme; spec.typeCount = 4; spec.animalCount = 8; spec.plantingCount = 3;
            world.Build(spec);
            var light = new GameObject("Preview sun").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.35f;
            light.transform.rotation = Quaternion.Euler(48,-32,0); light.shadows = LightShadows.None;
            Selection.activeGameObject = world.Root.gameObject;
            if (SceneView.lastActiveSceneView != null)
                SceneView.lastActiveSceneView.LookAt(Vector3.zero,Quaternion.Euler(90,0,0),29);
            Debug.Log("Unsaved map preview created additively. Generated previews close automatically before Play Mode or another preview.");
        }
    }
}
