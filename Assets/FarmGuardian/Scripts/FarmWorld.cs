using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace FarmGuardian
{
    public sealed class FarmWorld : MonoBehaviour
    {
        public const int SolidLayer = 8;
        public FarmSession Session;
        public Transform Root { get; private set; }
        readonly List<Material> materials = new List<Material>();
        readonly List<Mesh> generatedMeshes = new List<Mesh>();
        public FarmMapLayout Layout { get; private set; }
        public NavMeshSurface Surface { get; private set; }
        public FarmThiefExitZone ExitZone {get;private set;}
        readonly List<FarmPenGate> gates=new List<FarmPenGate>();
        public IReadOnlyList<FarmPenGate> Gates=>gates;
        public FarmPenGate GateFor(int type)=>type>=0&&type<gates.Count?gates[type]:null;
        readonly Dictionary<string, GameObject> townProps = new Dictionary<string, GameObject>();
        Camera mainCamera;
        FarmCamera follow;
        Material grass, path, wood, soil, leaf, stone, cream, blue;
        Transform environment, ground, boundaries, paths, decoration, buildings, pens, plotRoot, spawns, interactive, obstacles, crossings;

        public void Clear()
        {
            if (Root != null) { Root.gameObject.SetActive(false); Release(Root.gameObject); Root = null; }
            foreach (var material in materials) if (material != null) Release(material);
            foreach (var mesh in generatedMeshes) if (mesh != null) Release(mesh);
            generatedMeshes.Clear();
            gates.Clear();ExitZone=null;
            materials.Clear(); townProps.Clear(); Layout = null; Surface = null;
            if (follow != null) follow.Target = null;
            if (mainCamera != null) { mainCamera.transform.position = new Vector3(0, 22, -24); mainCamera.transform.LookAt(Vector3.zero); }
        }

        static void Release(Object value)
        { if (Application.isPlaying) Destroy(value); else DestroyImmediate(value); }

        Material ColorMaterial(string name, Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
            material.SetFloat("_Smoothness", 0.05f); materials.Add(material); return material;
        }

        Transform Group(string name, Transform parent)
        { var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform; }

        GameObject Shape(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Transform parent, bool solid = true)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name;
            go.transform.SetParent(parent, false); go.transform.localPosition = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if(name.EndsWith("boundary"))go.GetComponent<Renderer>().enabled=false;
            go.layer = solid ? SolidLayer : 0;
            if (!solid) { var c = go.GetComponent<Collider>(); c.enabled = false; Release(c); }
            else if (name != "Farm ground") NotWalkable(go);
            return go;
        }

        static void NotWalkable(GameObject go)
        {
            var modifier = go.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true; modifier.area = 1;
        }

        public void Build(LevelSpec spec)
        {
            Clear();
            Layout = FarmMapLayout.ForTheme(spec.theme);
            Root = new GameObject("Farm Guardian - " + spec.ThemeName).transform;
            environment = Group("Environment", Root);
            ground = Group("Ground", environment); boundaries = Group("Boundaries", environment);
            paths = Group("Paths", environment); decoration = Group("Decorations", environment);
            obstacles = Group("StrategicObstacles", environment); crossings = Group("Crossings", environment);
            buildings = Group("FarmBuildings", Root); pens = Group("AnimalPens", Root);
            plotRoot = Group("PlantingPlots", Root); spawns = Group("AnimalSpawnPoints", Root);
            interactive = Group("InteractiveObjects", Root);
            grass = ColorMaterial("Grass", spec.theme == 1 ? new Color(.29f,.53f,.23f) : spec.theme == 2 ? new Color(.40f,.58f,.29f) : new Color(.39f,.65f,.23f));
            path = ColorMaterial("Warm paths", new Color(0.80f, 0.66f, 0.42f));
            wood = ColorMaterial("Fence wood", new Color(0.54f, 0.32f, 0.18f));
            soil = ColorMaterial("Planting soil", new Color(0.28f, 0.16f, 0.10f));
            leaf = ColorMaterial("Plants", new Color(0.25f, 0.60f, 0.23f));
            stone = ColorMaterial("Mountain stone", new Color(0.48f, 0.54f, 0.56f));
            cream = ColorMaterial("Pen marker", new Color(0.98f, 0.87f, 0.59f));
            blue = ColorMaterial("Guardian marker", new Color(0.22f, 0.72f, 0.88f));

            Shape("Farm ground", PrimitiveType.Cube, new Vector3(0, -0.3f, 0), new Vector3(44, 0.6f, 36), grass, ground);
            Shape("North boundary", PrimitiveType.Cube, new Vector3(0, 1, 17), new Vector3(44, 2, 0.5f), wood, boundaries);
            Shape("South boundary", PrimitiveType.Cube, new Vector3(0, 1, -17), new Vector3(44, 2, 0.5f), wood, boundaries);
            Shape("West boundary", PrimitiveType.Cube, new Vector3(-21, 1, 0), new Vector3(0.5f, 2, 34), wood, boundaries);
            Shape("East boundary", PrimitiveType.Cube, new Vector3(21, 1, 0), new Vector3(0.5f, 2, 34), wood, boundaries);
            BuildRoutes();
            foreach (var item in Layout.Obstacles) BuildObstacle(item);
            foreach (var centre in Layout.CrossingCentres) BuildCrossing(centre);
            // Outside the boundary: silhouettes never steal clearance from a required route.
            for (int i = 0; i < 8; i++)
                DecorativeProp(spec.theme == 2 ? "Boulder" : spec.theme == 1 ? "Tree_Pine" : "Tree_Square",
                    new Vector3(i % 2 == 0 ? -23 : 23, 0, -12 + (i / 2) * 8), spec.theme == 2 ? 3 : 4);
            // Mountain scenery is outside the playable boundary, leaving actors on reliable level ground.
            if (spec.theme == 2)
                for (int i = 0; i < 7; i++) Shape("Distant mountain", PrimitiveType.Sphere, new Vector3(-23 + i * 8, 2, 23), new Vector3(10, 10 + i % 3 * 3, 9), stone, decoration, false);

            var homes = new List<Vector3>();
            // Empty future pens are scenery, not extra objectives or animals.
            for (int type = 0; type < Mathf.Min(Session.config.animalNames.Length,Layout.Pens.Length); type++)
            {
                Vector3 center = Layout.Pens[type];
                homes.Add(center);
                Transform pen = Group(Session.config.animalNames[type] + " Pen", pens);
                Shape("Pen floor", PrimitiveType.Cube, center + Vector3.up * 0.025f, new Vector3(3.5f, 0.04f, 4.5f), cream, pen, false);
                Fence(center + new Vector3(-1.8f, 0, 0), new Vector3(0.15f, 0.8f, 4.5f), pen);
                Fence(center + new Vector3(1.8f, 0, 0), new Vector3(0.15f, 0.8f, 4.5f), pen);
                Fence(center + new Vector3(0, 0, 2.25f), new Vector3(3.6f, 0.8f, 0.15f), pen);
                // A wide south-facing entrance prevents pathfinding dead ends.
                BuildPenSign(center,Session.config.animalNames[type].ToUpperInvariant(),pen);
            }
            for (int i = 0; i < spec.plantingCount; i++)
            {
                Transform plot = Group("Planting plot " + (i + 1), plotRoot);
                plot.position = Layout.Plots[i];
                Shape("Soil", PrimitiveType.Cube, new Vector3(0, 0.06f, 0), new Vector3(2.3f, 0.12f, 1.8f), soil, plot, false);
                var component = plot.gameObject.AddComponent<PlantingPlot>(); component.Session = Session;
                var crop = Group("Growing crops", plot);
                for (int j = 0; j < 4; j++) Shape("Seedling", PrimitiveType.Sphere, new Vector3(-0.7f + j * 0.45f, 0.4f, 0), new Vector3(0.25f, 0.7f, 0.25f), leaf, crop, false);
                crop.gameObject.SetActive(false); component.Crop = crop;
                WorldLabel("PLANT", plot.position + Vector3.up * 0.6f, plotRoot, Color.white);
                Session.Plots.Add(component);
            }
            Session.EscapePosition = Layout.Escape;
            var escape = Group("ThiefEscapeZone", Root); escape.position = Session.EscapePosition;
            ExitZone=escape.gameObject.AddComponent<FarmThiefExitZone>();
            Shape("Escape marker", PrimitiveType.Cube, new Vector3(0, 0.03f, 0), new Vector3(3, 0.04f, 3), wood, escape, false);
            WorldLabel("THIEF EXIT", Session.EscapePosition + Vector3.up * 0.7f, Root, Color.white);
            var playerSpawn = Group("PlayerSpawn", Root); playerSpawn.position = Layout.PlayerSpawn + Vector3.up * .05f;
            var hub = Group("ResponseHub", Root); hub.position = Layout.ResponseHub;
            var entrances = Group("ThiefEntrances", Root);
            for (int i = 0; i < Layout.ThiefEntrances.Length; i++)
            { var marker = Group("Entrance " + (i + 1), entrances); marker.position = Layout.ThiefEntrances[i]; }

            var scenery=Resources.Load<FarmScenerySet>("FarmScenery");
            if(scenery!=null&&scenery.themes!=null&&spec.theme<scenery.themes.Length&&scenery.themes[spec.theme]!=null)
            {
                var sceneryRoot=Instantiate(scenery.themes[spec.theme],Root,false);
                sceneryRoot.GetComponent<FarmSceneryStage>()?.Configure(spec.plantingCount);
            }
            Physics.SyncTransforms();
            Surface = Root.gameObject.AddComponent<NavMeshSurface>();
            Surface.collectObjects = CollectObjects.Children;
            Surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            Surface.layerMask = 1 << SolidLayer;
            Surface.overrideVoxelSize = true; Surface.voxelSize = .12f;
            Surface.overrideTileSize = true; Surface.tileSize = 128;
            Surface.BuildNavMesh();
            BuildPenGates();
            ValidateNavigation(spec);

            SpawnPlayer(playerSpawn.position);
            for (int i = 0; i < spec.animalCount; i++)
            {
                int type = i % spec.typeCount;
                GameObject go = Actor("Rescue " + Session.config.animalNames[type] + " " + (i + 1), Layout.AnimalSpawns[i], 1.1f);
                var animal = go.AddComponent<FarmAnimal>();
                animal.Session = Session; animal.TypeIndex = type;
                animal.WanderOrigin = go.transform.position;
                animal.Home = homes[type] + new Vector3((i / spec.typeCount % 2 == 0 ? -0.6f : 0.6f), 0, -0.8f + (i / (spec.typeCount * 2)) * 0.8f);
                var visual = Model(Session.config.animalModels[type], go.transform, type == 2 ? .65f : .95f, Session.config.animalMaterial);
                animal.Visual=visual;
                var animator = visual.GetComponentInChildren<Animator>();
                if (animator == null && visual.childCount > 0) animator = visual.GetChild(0).gameObject.AddComponent<Animator>();
                if (animator != null && type < Session.config.animalAnimators.Length)
                { animator.runtimeAnimatorController = Session.config.animalAnimators[type]; animator.applyRootMotion = false; animal.Animator = animator; }
                var spawn = Group("Animal spawn " + i, spawns); spawn.position = go.transform.position;
                Session.Animals.Add(animal);
            }
            GameObject thiefGo = Actor("Burglar", ChooseThiefSpawn(Vector3.zero, false), spec.thiefSpeed);
            var thief = thiefGo.AddComponent<FarmThief>(); thief.Session = Session;
            thief.Visual = Model(Session.config.thiefModel, thiefGo.transform, 1.8f, Session.config.thiefMaterial);
            thief.WalkAnimation = thief.Visual.GetComponentInChildren<FarmCharacterAnimation>();
            Session.Thief = thief;
            Session.PenPosition = homes[0];
            SetupCamera(); follow.Target = Session.Player.transform;
        }

        public void RandomizeThiefSpawn(NavMeshAgent thief)
        {
            if (thief == null || !thief.isOnNavMesh) return;
            thief.Warp(ChooseThiefSpawn(thief.transform.position, true));
            thief.ResetPath();
        }

        void BuildRoutes()
        {
            var main = Group("Main routes", paths); var alternative = Group("Alternative routes", paths);
            foreach (var route in Layout.Routes)
            {
                var parent = Group(route.Name, route.Alternative ? alternative : main);
                var vertices = new List<Vector3>(); var triangles = new List<int>();
                for (int i = 1; i < route.Nodes.Length; i++)
                {
                    Vector3 delta = route.Nodes[i] - route.Nodes[i - 1];
                    Vector3 side = Vector3.Cross(Vector3.up,delta.normalized)*route.Width*.5f;
                    int index = vertices.Count;
                    vertices.Add(route.Nodes[i-1]-side); vertices.Add(route.Nodes[i]-side);
                    vertices.Add(route.Nodes[i]+side); vertices.Add(route.Nodes[i-1]+side);
                    triangles.AddRange(new[] {index,index+1,index+2,index,index+2,index+3});
                }
                foreach (var point in route.Nodes)
                {
                    int index = vertices.Count; vertices.Add(point);
                    for (int i = 0; i <= 16; i++)
                    {
                        float angle = i*Mathf.PI*2/16;
                        vertices.Add(point+new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle))*route.Width*.5f);
                    }
                    for (int i = 0; i < 16; i++) triangles.AddRange(new[] {index,index+i+2,index+i+1});
                }
                var mesh = new Mesh { name = route.Name + " rounded lane" };
                mesh.SetVertices(vertices); mesh.SetTriangles(triangles,0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
                generatedMeshes.Add(mesh);
                parent.localPosition = Vector3.up*(route.Alternative ? .017f : .02f);
                parent.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = parent.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterial = path;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        void BuildObstacle(MapObstacle item)
        {
            if (item.Asset == "Fence")
            {
                var parent = Group(item.Name, obstacles);
                Fence(item.Position, item.Size, parent); return;
            }
            if (item.Asset == "Grove" || item.Asset == "Ridge")
            {
                var parent = Group(item.Name, obstacles);
                Shape(item.Asset == "Grove" ? "Solid undergrowth" : "Rock face", PrimitiveType.Cube,
                    item.Position + Vector3.up * item.Size.y * .5f, item.Size, item.Asset == "Grove" ? leaf : stone, parent);
                var source = FindTownProp(item.Asset == "Grove" ? "Tree_Pine" : "Boulder");
                for (int i = 0; i < 4; i++)
                {
                    var detail = Group("Landmark detail " + i, parent);
                    detail.position = item.Position + new Vector3(i % 2 == 0 ? -item.Size.x * .24f : item.Size.x * .24f,
                        item.Asset == "Grove" ? .2f : item.Size.y * .6f, i < 2 ? -item.Size.z * .26f : item.Size.z * .26f);
                    if (source != null) Model(source, detail, item.Asset == "Grove" ? 3.8f : 1.7f, Session.config.environmentMaterial);
                }
                return;
            }
            Prop(item.Asset, item.Position, item.Size.y, item.Size, item.Name);
        }

        void BuildCrossing(Vector3 centre)
        {
            var parent = Group(centre.z < 0 ? "South timber causeway" : "North timber pass", crossings);
            for (int i = 0; i < 9; i++)
                Shape("Timber plank", PrimitiveType.Cube, centre + new Vector3(-3.2f + i * .8f,.02f,0),
                    new Vector3(.76f,.035f,3.4f), wood, parent, false);
            Fence(centre + Vector3.forward * 1.98f, new Vector3(7,.55f,.14f), parent);
            Fence(centre + Vector3.back * 1.98f, new Vector3(7,.55f,.14f), parent);
        }

        static void FitFootprint(Transform visual, Vector3 size)
        {
            var renderers = visual.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float fit = Mathf.Min(1, size.x / Mathf.Max(.01f,bounds.size.x), size.z / Mathf.Max(.01f,bounds.size.z));
            visual.localScale *= fit;
        }

        public static Vector3 RequirePoint(Vector3 point, string name)
        {
            if (NavMesh.SamplePosition(point, out var hit, .4f, NavMesh.AllAreas) &&
                Vector3.Distance(new Vector3(hit.position.x,point.y,hit.position.z),point) < .2f)
                return hit.position;
            throw new System.InvalidOperationException("Map location is blocked or outside navigation: " + name + " " + point);
        }

        public static float RouteLength(Vector3 start, Vector3 end)
        {
            var route = new NavMeshPath();
            if (!NavMesh.CalculatePath(start,end,NavMesh.AllAreas,route) || route.status != NavMeshPathStatus.PathComplete)
                return float.PositiveInfinity;
            float length = 0;
            for (int i = 1; i < route.corners.Length; i++) length += Vector3.Distance(route.corners[i-1],route.corners[i]);
            return length;
        }

        public void ValidateNavigation(LevelSpec spec)
        {
            var player = RequirePoint(Layout.PlayerSpawn,"Player spawn");
            var exit = RequirePoint(Layout.Escape,"Escape");
            RequirePoint(Layout.ResponseHub,"Response hub");
            var targets = new List<Vector3> { exit, Layout.ResponseHub };
            targets.AddRange(Layout.AnimalSpawns); targets.AddRange(Layout.Plots); targets.AddRange(Layout.ThiefEntrances);
            for (int i = 0; i < spec.typeCount; i++) targets.Add(Layout.Pens[i]);
            foreach (var target in targets)
                if (float.IsInfinity(RouteLength(player,RequirePoint(target,"Required destination"))))
                    throw new System.InvalidOperationException("Disconnected map destination on " + Layout.Name + ": " + target);
            for (int i = 0; i < spec.animalCount; i++)
            {
                var home = Layout.Pens[i % spec.typeCount] + new Vector3(i / spec.typeCount % 2 == 0 ? -.6f : .6f,0,
                    -.8f + (i / (spec.typeCount * 2)) * .8f);
                RequirePoint(home,"Animal home");
                if (float.IsInfinity(RouteLength(Layout.AnimalSpawns[i],home)) || float.IsInfinity(RouteLength(home,exit)))
                    throw new System.InvalidOperationException("Animal or thief route is incomplete on " + Layout.Name);
            }
            foreach (var route in Layout.Routes)
                for (int i = 1; i < route.Nodes.Length; i++)
                {
                    var start = RequirePoint(route.Nodes[i-1],route.Name);
                    var end = RequirePoint(route.Nodes[i],route.Name);
                    Vector3 delta = end-start;
                    bool physicalBlock = Physics.CapsuleCast(start+Vector3.up*.4f,start+Vector3.up*1.4f,.34f,delta.normalized,
                        out var collision,delta.magnitude,1<<SolidLayer);
                    // A ray along a triangulation/tile edge can report a hit even on an open apron.
                    // Require both a complete nearly-straight agent route and a clear player-sized sweep.
                    float distance = RouteLength(start,end);
                    if (physicalBlock || distance > delta.magnitude+Mathf.Max(.8f,delta.magnitude*.05f))
                    {
                        var diagnostic=new NavMeshPath();NavMesh.CalculatePath(start,end,NavMesh.AllAreas,diagnostic);
                        throw new System.InvalidOperationException("Authored lane crosses collision: " + Layout.Name + " / " + route.Name + " segment " + i +
                            " (direct " + delta.magnitude + ", route " + distance + ", collider " + (physicalBlock ? collision.collider.name : "none") + ", corners "+string.Join("; ",System.Array.ConvertAll(diagnostic.corners,p=>p.ToString()))+")");
                    }
                }
        }

        bool IsValidThiefSpawn(Vector3 position)
        {
            if (Session.Player != null && Vector3.Distance(Session.Player.transform.position,position) < 8) return false;
            if (Session.Animals.Exists(a => Vector3.Distance(a.Home,position) < 6)) return false;
            if (float.IsInfinity(RouteLength(position,Session.EscapePosition))) return false;
            foreach (var animal in Session.Animals) if (float.IsInfinity(RouteLength(position,animal.Home))) return false;
            return true;
        }

        public Vector3 ChooseThiefSpawn(Vector3 previous, bool avoidPrevious)
        {
            // Perimeter entrances are curated; selection and small offsets vary on every run/activation.
            var choices = new List<Vector3>();
            foreach (Vector3 entrance in Layout.ThiefEntrances)
            {
                Vector3 candidate = entrance + new Vector3(Random.Range(-0.7f, 0.7f), 0, Random.Range(-0.7f, 0.7f));
                if (!NavMesh.SamplePosition(candidate, out var hit, 1.5f, NavMesh.AllAreas)) continue;
                if (avoidPrevious && Vector3.Distance(previous, hit.position) < 4) continue;
                if (IsValidThiefSpawn(hit.position)) choices.Add(hit.position);
            }
            if (choices.Count > 0) return choices[Random.Range(0, choices.Count)];
            // Last valid position is safer than forcing a spawn into an obstacle.
            if (avoidPrevious && IsValidThiefSpawn(previous)) return previous;
            throw new System.InvalidOperationException("No reachable thief spawn was found on this map.");
        }

        void Fence(Vector3 center, Vector3 size, Transform parent)
        {
            // One continuous collision footprint, with light rails/posts instead of an opaque wall.
            var solid = new GameObject("Solid fence collider"); solid.transform.SetParent(parent,false);
            solid.transform.localPosition = center + Vector3.up*size.y*.5f; solid.layer = SolidLayer;
            solid.AddComponent<BoxCollider>().size = size; NotWalkable(solid);
            bool alongX = size.x >= size.z;
            float length = alongX ? size.x : size.z;
            var suppliedFence=FindTownProp("Fence");
            if(suppliedFence!=null)
            {
                int modules=Mathf.CeilToInt(length/1.6f);float span=length/modules;
                for(int i=0;i<modules;i++)
                {
                    var slot=Group("Supplied wooden fence section",parent);
                    slot.localPosition=center+(alongX?Vector3.right:Vector3.forward)*(-length*.5f+span*(i+.5f));
                    var visual=Model(suppliedFence,slot,size.y+.1f,Session.config.environmentMaterial);
                    var renderers=visual.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
                    visual.localScale=new Vector3(span/Mathf.Max(.01f,bounds.size.x),1,.18f/Mathf.Max(.01f,bounds.size.z));
                    if(!alongX)visual.localRotation=Quaternion.Euler(0,90,0);
                }
                return;
            }
            int posts = Mathf.Max(1,Mathf.CeilToInt(length/2.4f));
            for (int i = 0; i <= posts; i++)
            {
                Vector3 offset = (alongX ? Vector3.right : Vector3.forward)*Mathf.Lerp(-length*.5f,length*.5f,i/(float)posts);
                Shape("Fence post",PrimitiveType.Cube,center+offset+Vector3.up*(size.y+.1f)*.5f,
                    new Vector3(.17f,size.y+.1f,.17f),wood,parent,false);
            }
            for (int i = 0; i < 2; i++)
                Shape("Fence rail",PrimitiveType.Cube,center+Vector3.up*size.y*(i == 0 ? .34f : .86f),
                    alongX ? new Vector3(length,.12f,.12f) : new Vector3(.12f,.12f,length),wood,parent,false);
        }

        void BuildPenSign(Vector3 center,string title,Transform pen)
        {
            var sign=Group("Wooden sign - "+title,pen);
            Vector3 p=center+new Vector3(0,1.65f,1.85f);
            for(int i=-1;i<=1;i+=2)Shape("Sign post",PrimitiveType.Cube,center+new Vector3(i*1.2f,.8f,1.85f),new Vector3(.12f,1.65f,.12f),wood,sign,false);
            Shape("Wooden name board",PrimitiveType.Cube,p,new Vector3(3,.7f,.15f),wood,sign,false);
            WorldLabel(title,p+new Vector3(0,0,-.11f),sign,new Color(1,.95f,.83f));
            sign.Find(title).rotation=Quaternion.identity;
        }
        void BuildPenGates()
        {
            // Bake the traversable doorway first. Runtime leaves control physical passage;
            // agents request opening on approach, avoiding a rebake or blocked objective path.
            for(int type=0;type<Layout.Pens.Length;type++)
            {
                var root=Group("Opening gate - "+Session.config.animalNames[type],pens.GetChild(type));
                root.position=Layout.Pens[type]+Vector3.back*2.25f;
                var gate=root.gameObject.AddComponent<FarmPenGate>();gate.Session=Session;gate.TypeIndex=type;gate.PenCenter=Layout.Pens[type];
                gate.Left=Group("Left hinged leaf",root);gate.Left.localPosition=Vector3.left*1.8f;
                gate.Right=Group("Right hinged leaf",root);gate.Right.localPosition=Vector3.right*1.8f;
                for(int side=0;side<2;side++)
                {
                    var parent=side==0?gate.Left:gate.Right;float direction=side==0?1:-1;
                    for(int rail=0;rail<2;rail++)Shape("Gate wooden rail",PrimitiveType.Cube,new Vector3(direction*.9f,.28f+rail*.4f,0),new Vector3(1.8f,.14f,.14f),wood,parent,false);
                    for(int post=0;post<3;post++)Shape("Gate upright",PrimitiveType.Cube,new Vector3(direction*(.08f+post*.8f),.45f,0),new Vector3(.13f,.9f,.13f),wood,parent,false);
                }
                var barrier=new GameObject("Gate doorway collision");barrier.transform.SetParent(root,false);barrier.layer=SolidLayer;
                var box=barrier.AddComponent<BoxCollider>();box.center=Vector3.up*.45f;box.size=new Vector3(3.6f,.9f,.16f);gate.Barrier=box;
                barrier.AddComponent<NavMeshModifier>().ignoreFromBuild=true;gates.Add(gate);
            }
        }

        public GameObject FindTownProp(string name)
        {
            if (townProps.TryGetValue(name, out var cached)) return cached;
            if (Session.config.townModel == null) return null;
            foreach (Transform t in Session.config.townModel.GetComponentsInChildren<Transform>(true))
                if (t.name == name) { townProps[name] = t.gameObject; return t.gameObject; }
            return null;
        }

        void Prop(string name, Vector3 position, float height, Vector3 colliderSize, string label = null)
        {
            Transform group = Group(label ?? name, buildings); group.position = position;
            var source = FindTownProp(name);
            if (source != null)
            {
                var visual = Model(source, group, height, Session.config.environmentMaterial);
                FitFootprint(visual, colliderSize);
            }
            else Shape(name + " primitive fallback", PrimitiveType.Cube, new Vector3(0, height / 2, 0), colliderSize, wood, group, false);
            GameObject box = new GameObject("Obstacle collider"); box.transform.SetParent(group, false);
            box.layer = SolidLayer;
            var collider = box.AddComponent<BoxCollider>(); collider.size = colliderSize; collider.center = new Vector3(0, colliderSize.y / 2, 0);
            NotWalkable(box);
        }

        void DecorativeProp(string name, Vector3 position, float height)
        {
            Transform group = Group(name, decoration); group.position = position;
            var source = FindTownProp(name);
            if (source != null) Model(source, group, height, Session.config.environmentMaterial);
            else Shape(name + " fallback", PrimitiveType.Sphere, Vector3.up * height / 2, new Vector3(1.5f, height, 1.5f), leaf, group, false);
        }

        GameObject Actor(string name, Vector3 position, float speed)
        {
            GameObject go = new GameObject(name); go.transform.SetParent(interactive, false);
            go.transform.position = RequirePoint(position, name);
            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.28f; agent.height = 1.6f; agent.speed = speed; agent.angularSpeed = 600;
            agent.acceleration = 14; agent.stoppingDistance = 0.25f;
            return go;
        }

        void SpawnPlayer(Vector3 position)
        {
            var go = new GameObject("Farm Guardian Player"); go.transform.SetParent(interactive, false); go.transform.position = position;
            var controller = go.AddComponent<CharacterController>();
            controller.height = 1.8f; controller.radius = 0.32f; controller.center = Vector3.up * 0.9f;
            controller.stepOffset = 0.25f; controller.skinWidth = 0.04f;
            var player = go.AddComponent<FarmPlayer>(); player.Session = Session;
            string skinId=Session.Save.EquippedSkinId;
            player.Visual = Model(FarmSkinCatalog.Model(Session.config,skinId), go.transform, 1.8f, FarmSkinCatalog.Material(Session.config,skinId));
            player.WalkAnimation = player.Visual.GetComponentInChildren<FarmCharacterAnimation>();
            if (skinId==FarmSkinCatalog.GoldenFarmer)
            {
                var tint = new Material(Session.config.farmerMaterial) { color = new Color(1, 0.8f, 0.4f) }; materials.Add(tint);
                foreach (var renderer in player.Visual.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = tint;
            }
            Session.Player = player;
        }

        Transform Model(GameObject source, Transform parent, float height, Material material)
        {
            Transform wrapper = Group("Visual", parent);
            if(source!=null&&source.name=="Windmill")
            {
                var art=Resources.Load<FarmWindmillAsset>("FarmWindmill");
                if(art!=null&&art.Body!=null&&art.Rotor!=null)
                {
                    var body=Group("Original windmill tower",wrapper);body.gameObject.AddComponent<MeshFilter>().sharedMesh=art.Body;
                    body.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
                    var rotor=Group("Original rotating windmill blades",wrapper);rotor.localPosition=art.Pivot;
                    rotor.gameObject.AddComponent<MeshFilter>().sharedMesh=art.Rotor;rotor.gameObject.AddComponent<MeshRenderer>().sharedMaterial=material;
                    var motion=rotor.gameObject.AddComponent<FarmWindmillRotor>();motion.Session=Session;motion.Axis=art.Axis;
                    wrapper.localScale=Vector3.one*height;return wrapper;
                }
            }
            if (source == null) { Shape("Missing model fallback", PrimitiveType.Capsule, Vector3.up * height / 2, new Vector3(0.6f, height / 2, 0.6f), cream, wrapper, false); return wrapper; }
            var instance = Instantiate(source, wrapper);
            instance.name = source.name;
            instance.transform.localPosition = Vector3.zero;
            // Preserve source rotation: town submeshes can have authored axis conversion.
            foreach (var collider in instance.GetComponentsInChildren<Collider>()) { collider.enabled = false; Release(collider); }
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                if (material != null)
                {
                    var slots = renderer.sharedMaterials;
                    for (int i = 0; i < slots.Length; i++) slots[i] = material;
                    renderer.sharedMaterials = slots;
                }
                renderer.shadowCastingMode = ShadowCastingMode.On;
            }
            // Generated character prefabs already use metres and a feet-at-origin pivot.
            // Their expanded skinned culling bounds must not be used to resize the character.
            if (instance.GetComponent<FarmCharacterAnimation>() != null) return wrapper;
            var renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                instance.transform.localScale *= height / Mathf.Max(0.01f, bounds.size.y);
                bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                instance.transform.position += new Vector3(parent.position.x - bounds.center.x, parent.position.y - bounds.min.y, parent.position.z - bounds.center.z);
            }
            return wrapper;
        }

        void WorldLabel(string text, Vector3 position, Transform parent, Color color)
        {
            var go = new GameObject(text); go.transform.SetParent(parent, false); go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(55, 0, 0);
            var label = go.AddComponent<TextMesh>(); label.text = text; label.fontSize = 40; label.characterSize = 0.08f;
            var bubbleFont=FarmUiTypography.BubbleSource;
            if(bubbleFont!=null)
            {
                label.font=bubbleFont;label.fontStyle=FontStyle.Normal;
                go.GetComponent<MeshRenderer>().sharedMaterial=bubbleFont.material;
            }
            label.anchor = TextAnchor.MiddleCenter; label.color = color;
        }

        void SetupCamera()
        {
            mainCamera = null;
            foreach (var candidate in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (candidate.CompareTag("MainCamera") && candidate.gameObject.scene == Root.gameObject.scene) { mainCamera = candidate; break; }
            if (mainCamera == null)
            {
                var go = new GameObject("Main Camera"); go.tag = "MainCamera";
                mainCamera = go.AddComponent<Camera>(); go.AddComponent<AudioListener>();
            }
            mainCamera.backgroundColor = new Color(0.67f, 0.81f, 0.84f);
            mainCamera.fieldOfView = 52; mainCamera.farClipPlane = 130;
            mainCamera.orthographic=false;
            follow = mainCamera.GetComponent<FarmCamera>();
            if (follow == null) follow = mainCamera.gameObject.AddComponent<FarmCamera>();
            follow.Offset=new Vector3(0,11.5f,-9.5f);mainCamera.fieldOfView=48;
            SetupDaylight();
            mainCamera.transform.position = Session.Player.transform.position + follow.Offset;
        }
        void SetupDaylight()
        {
            Light sun=null;
            foreach(var light in FindObjectsByType<Light>(FindObjectsSortMode.None))if(light.type==LightType.Directional){sun=light;break;}
            if(sun==null){var go=new GameObject("Farm daylight");sun=go.AddComponent<Light>();sun.type=LightType.Directional;}
            sun.transform.rotation=Quaternion.Euler(48,-32,0);sun.color=new Color(1,.94f,.83f);sun.intensity=1.15f;
            sun.shadows=LightShadows.Soft;sun.shadowStrength=.72f;sun.shadowBias=.035f;sun.shadowNormalBias=.25f;
            RenderSettings.sun=sun;RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.62f,.72f,.84f);RenderSettings.ambientEquatorColor=new Color(.42f,.47f,.39f);RenderSettings.ambientGroundColor=new Color(.28f,.31f,.24f);
            foreach(var renderer in Root.GetComponentsInChildren<Renderer>())
            {renderer.receiveShadows=true;if(renderer.bounds.size.y>1.5f)renderer.shadowCastingMode=ShadowCastingMode.On;}
            var wind=Root.gameObject.AddComponent<FarmEnvironmentMotion>();wind.Session=Session;
            var windShader=Shader.Find("FarmGuardian/FoliageWind");
            if(windShader!=null)
            {
                var foliage=new Material(Session.config.environmentMaterial){name="Existing leaf palette - gentle wind",shader=windShader};materials.Add(foliage);
                foreach(var renderer in Root.GetComponentsInChildren<MeshRenderer>())
                {
                    bool leaves=false;for(var t=renderer.transform;t!=null&&t!=Root;t=t.parent)
                        if(t.name.StartsWith("Tree_")||t.name.Contains("woods")||t.name.Contains("groves")){leaves=true;break;}
                    if(leaves)renderer.sharedMaterial=foliage;
                }
            }
        }
    }
}
