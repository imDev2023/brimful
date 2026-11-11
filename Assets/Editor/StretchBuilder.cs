// Build the prototype stretch (SPEC.md, "How it is made", step 4) as Assets/Scenes/Stretch.unity,
// entirely from code, so the level can be rebuilt and changed safely:
//
//   unity command script eval 'return Brimful.EditorTools.StretchBuilder.Build();'
//
// The route, in metres, the hero starting at the origin facing +Z:
//   1. the dry basin (y 0) to a wall whose left part is a climbing face of holds (z 4);
//   2. climb to the lip (y 4), where a watch house on the terrace stands over the climbing face,
//      so she shimmies right along the lip to open ground and pulls up;
//   3. run and jump the 2.3 m gap to the sluice platform (z 10.8);
//   4. turn the wheel: the gate lifts and water pours into the dry cistern on the right,
//      lifting the crate on its floor up to the platform's height;
//   5. jump onto the crate, then leap for the lip of the high arcade (y 5.4) and pull up.
//
// Colours are the concept video's, measured in SPEC.md; textures are painted procedurally here
// (ours, CC0) and mapped at real size through box meshes whose UVs are in metres.

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Brimful.EditorTools
{
    public static class StretchBuilder
    {
        const string EnvDir = "Assets/Environment";
        const string ScenePath = "Assets/Scenes/Stretch.unity";
        const float TileMeters = 2f;

        static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.magenta;

        public static string Build()
        {
            meshes.Clear();
            mats.Clear();
            Folder(EnvDir);
            Folder($"{EnvDir}/Meshes");
            Folder($"{EnvDir}/Textures");
            Folder($"{EnvDir}/Materials");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var plaster = PaintedTexture("painted_plaster", 11, 3.0f, 0.22f);
            var stone = PaintedTexture("painted_stone", 23, 1.4f, 0.28f);
            var adobe = Mat("Adobe", Hex("#d7945d"), plaster);
            var adobeLight = Mat("AdobeLight", Hex("#e3a36c"), plaster);
            var brick = Mat("Brick", Hex("#c8703f"), stone);
            var stoneTop = Mat("StoneTop", Hex("#e6c48a"), stone);
            var sand = Mat("Sand", Hex("#e9c896"), plaster);
            var wood = Mat("Wood", Hex("#875f3d"), stone);
            var crateWood = Mat("CrateWood", Hex("#bf9666"), stone);
            var water = WaterMat();

            var world = new GameObject("World").transform;
            var level = new GameObject("Level").transform;
            level.SetParent(world);

            // The desert the city stands on, beyond the play space.
            Box("Desert", new Vector3(-160f, -1.5f, -160f), new Vector3(160f, -0.5f, 160f), sand, world, collider: false);

            // 1. The basin and its low boundary walls.
            Box("Basin", new Vector3(-8f, -0.5f, -7f), new Vector3(8f, 0f, 4.01f), sand, level);
            Box("BasinWallW", new Vector3(-8.6f, -0.5f, -7f), new Vector3(-8f, 1.4f, 4f), adobe, level);
            Box("BasinWallE", new Vector3(8f, -0.5f, -7f), new Vector3(8.6f, 1.4f, 9.5f), adobe, level);
            Box("BasinWallS", new Vector3(-8.6f, -0.5f, -7.6f), new Vector3(8.6f, 1.4f, -7f), adobe, level);

            // 2. The terrace block, its climbing face and lip, and the watch house on top.
            Box("Terrace", new Vector3(-3.5f, -0.5f, 4f), new Vector3(3.5f, 4f, 8.5f), adobe, level);
            Box("TerraceCoping", new Vector3(-3.55f, 3.9f, 3.92f), new Vector3(3.55f, 4.02f, 8.5f), stoneTop, level);
            Box("TerraceWingW", new Vector3(-8f, -0.5f, 4f), new Vector3(-3.5f, 6.5f, 10.8f), adobeLight, level);
            Box("TerraceWingE", new Vector3(3.5f, -0.5f, 4f), new Vector3(8.6f, 6.5f, 8.9f), adobeLight, level);
            var face = new GameObject("ClimbFace");
            face.transform.SetParent(level);
            var faceCol = face.AddComponent<BoxCollider>();
            faceCol.center = new Vector3(-1.85f, 1.95f, 3.86f);
            faceCol.size = new Vector3(2.7f, 3.7f, 0.08f);
            face.AddComponent<ClimbSurface>();
            Holds(level, brick, stoneTop);
            AddLedge(level, "TerraceLip", new Vector3(0f, 4.02f, 3.92f), Quaternion.Euler(0f, 180f, 0f), 3.3f);
            Box("WatchHouse", new Vector3(-3.5f, 4f, 4.25f), new Vector3(0.65f, 6.6f, 6.8f), adobeLight, level);
            Box("WatchHouseRoof", new Vector3(-3.65f, 6.6f, 4.1f), new Vector3(0.8f, 6.8f, 6.95f), stoneTop, level);
            Box("WatchHouseDoor", new Vector3(-1.9f, 4f, 4.2f), new Vector3(-1.1f, 5.6f, 4.26f), wood, level, collider: false);

            // 3. The gap (a dry channel below) and the sluice platform.
            Box("GapFloor", new Vector3(-8f, -0.5f, 8.5f), new Vector3(3.5f, 0f, 10.8f), sand, level);
            Box("Platform", new Vector3(-3.5f, -0.5f, 10.8f), new Vector3(3.5f, 4f, 14f), adobe, level);
            Box("PlatformCoping", new Vector3(-3.55f, 3.9f, 10.72f), new Vector3(3.55f, 4.02f, 14f), stoneTop, level);
            Box("PlatformWingW", new Vector3(-8f, -0.5f, 10.8f), new Vector3(-3.5f, 6.5f, 15.5f), adobeLight, level);
            Box("Reservoir", new Vector3(-4f, -0.5f, 14f), new Vector3(3.5f, 7f, 15.5f), adobeLight, level);
            Box("ReservoirCap", new Vector3(-4.1f, 7f, 13.9f), new Vector3(3.6f, 7.2f, 15.6f), stoneTop, level);

            // 4. The trough from the reservoir, the sluice gate over the cistern, and the wheel.
            Box("TroughFloor", new Vector3(0.6f, 4.02f, 13.05f), new Vector3(3.5f, 4.12f, 13.95f), stoneTop, level);
            Box("TroughWallFront", new Vector3(0.6f, 4.02f, 12.95f), new Vector3(3.5f, 4.55f, 13.1f), stoneTop, level);
            var sluiceRoot = new GameObject("Sluice").transform;
            sluiceRoot.SetParent(level);
            Box("GatePostN", new Vector3(3.35f, 4.02f, 13.95f), new Vector3(3.55f, 5.9f, 14.1f), wood, sluiceRoot);
            Box("GatePostS", new Vector3(3.35f, 4.02f, 12.9f), new Vector3(3.55f, 5.9f, 13.05f), wood, sluiceRoot);
            Box("GateBeam", new Vector3(3.3f, 5.9f, 12.85f), new Vector3(3.6f, 6.1f, 14.15f), wood, sluiceRoot);
            var gate = Box("GateLeaf", new Vector3(3.38f, 4.1f, 13.08f), new Vector3(3.5f, 4.75f, 13.92f), wood, sluiceRoot);
            var wheelPivot = new GameObject("WheelPivot").transform;
            wheelPivot.SetParent(sluiceRoot);
            wheelPivot.position = new Vector3(2.55f, 4.9f, 12.75f);
            Box("WheelPost", new Vector3(2.47f, 4.02f, 12.82f), new Vector3(2.63f, 4.9f, 12.98f), wood, sluiceRoot);
            for (int i = 0; i < 12; i++)
            {
                // The rim, as twelve short staves around the spokes.
                float a = i * 30f * Mathf.Deg2Rad;
                var stave = Box($"WheelRim{i}", new Vector3(-0.115f, -0.04f, -0.035f), new Vector3(0.115f, 0.04f, 0.035f), wood, wheelPivot, collider: false);
                stave.transform.localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.42f;
                stave.transform.localRotation = Quaternion.Euler(0f, 0f, i * 30f + 90f);
            }
            for (int i = 0; i < 4; i++)
            {
                var spoke = Box($"WheelSpoke{i}", new Vector3(-0.42f, -0.035f, -0.03f), new Vector3(0.42f, 0.035f, 0.03f), wood, wheelPivot, collider: false);
                spoke.transform.localPosition = Vector3.zero;
                spoke.transform.localRotation = Quaternion.Euler(0f, 0f, i * 45f);
            }
            var stand = new GameObject("StandPoint").transform;
            stand.SetParent(sluiceRoot);
            // 0.4 m in front of the wheel: close enough for both hands on its rim.
            stand.SetPositionAndRotation(new Vector3(2.55f, 4.02f, 12.35f), Quaternion.LookRotation(Vector3.forward));
            var outflow = Box("Outflow", new Vector3(3.55f, 0f, 13.15f), new Vector3(3.75f, 4.3f, 13.85f), water, sluiceRoot, collider: false);
            outflow.SetActive(false);   // shown when the gate lifts

            // The cistern: walls on three sides, the platform's flank on the fourth.
            Box("CisternFloor", new Vector3(3.5f, -0.5f, 9.5f), new Vector3(8.5f, 0f, 15f), sand, level);
            Box("CisternWallN", new Vector3(3.5f, -0.5f, 15f), new Vector3(8.5f, 4.6f, 15.6f), adobe, level);
            Box("CisternWallS", new Vector3(3.5f, -0.5f, 8.9f), new Vector3(8.5f, 4.6f, 9.5f), adobe, level);
            var waterBody = new GameObject("CisternWater").AddComponent<WaterBody>();
            waterBody.transform.SetParent(level);
            waterBody.emptyLevel = -0.05f;
            waterBody.fullLevel = 3.7f;
            waterBody.fillSeconds = 9f;
            waterBody.area = new Bounds(new Vector3(6f, 2f, 12.25f), new Vector3(5f, 6f, 5.5f));
            var surface = Box("Surface", new Vector3(3.5f, -0.02f, 9.5f), new Vector3(8.5f, 0f, 15f), water, waterBody.transform, collider: false);
            waterBody.surface = surface.transform;
            surface.SetActive(false);   // the cistern starts dry

            var crate = Box("Crate", new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f), crateWood, level);
            crate.transform.position = new Vector3(6f, 0.5f, 12.2f);
            crate.AddComponent<Rigidbody>();
            var floater = crate.AddComponent<Floater>();
            floater.water = waterBody;
            floater.restY = 0.5f;
            floater.draft = 0.25f;

            var sluice = sluiceRoot.gameObject.AddComponent<Sluice>();
            sluice.wheel = wheelPivot;
            sluice.gate = gate.transform;
            sluice.gateLift = 1.0f;
            sluice.standPoint = stand;
            sluice.outflow = outflow;
            sluice.water = waterBody;

            // 5. The high arcade beyond the cistern, its lip, and the arches along it.
            Box("Arcade", new Vector3(8.5f, -0.5f, 8.9f), new Vector3(14f, 5.4f, 15.6f), adobe, level);
            Box("ArcadeCoping", new Vector3(8.42f, 5.3f, 8.9f), new Vector3(14f, 5.42f, 15.6f), stoneTop, level);
            AddLedge(level, "ArcadeLip", new Vector3(8.42f, 5.42f, 12.25f), Quaternion.Euler(0f, -90f, 0f), 2.4f);
            for (int i = 0; i < 3; i++)
            {
                float z = 9.6f + i * 2.1f;
                Box($"ArcadePier{i}", new Vector3(13.2f, 5.4f, z), new Vector3(13.8f, 8.2f, z + 0.6f), brick, level);
            }
            Box("ArcadeLintel", new Vector3(13.1f, 8.2f, 9.4f), new Vector3(13.9f, 8.8f, 14.6f), brick, level);
            Box("ArcadeBack", new Vector3(14f, -0.5f, 8.9f), new Vector3(14.6f, 6.6f, 15.6f), adobeLight, level);
            Box("ArcadeParapetS", new Vector3(8.5f, 5.42f, 8.9f), new Vector3(14f, 5.95f, 9.25f), stoneTop, level);
            Box("ArcadeParapetN", new Vector3(8.5f, 5.42f, 15.25f), new Vector3(14f, 5.95f, 15.6f), stoneTop, level);

            // Dressing: a city of adobe blocks and an aqueduct of arches beyond.
            City(world, adobe, adobeLight, brick, stoneTop);

            // Zones: checkpoints, kill zones and the goal.
            var zones = new GameObject("Zones").transform;
            zones.SetParent(world);
            Zone(zones, "StartCheckpoint", ZoneKind.Checkpoint, new Vector3(0f, 1f, -1.5f), new Vector3(16f, 2.5f, 11f), new Vector3(0f, 0f, -3f));
            Zone(zones, "TerraceCheckpoint", ZoneKind.Checkpoint, new Vector3(2f, 5f, 6.4f), new Vector3(3f, 2f, 4f), new Vector3(2f, 4.02f, 6.5f));
            Zone(zones, "PlatformCheckpoint", ZoneKind.Checkpoint, new Vector3(0f, 5f, 12.4f), new Vector3(7f, 2f, 3.2f), new Vector3(0f, 4.02f, 11.6f));
            Zone(zones, "GapKill", ZoneKind.Kill, new Vector3(-2.25f, 0.3f, 9.65f), new Vector3(11.5f, 1.4f, 2.3f), Vector3.zero);
            Zone(zones, "DryCisternKill", ZoneKind.Kill, new Vector3(6f, 0.4f, 12.25f), new Vector3(5f, 0.9f, 6.1f), Vector3.zero);
            Zone(zones, "Goal", ZoneKind.Goal, new Vector3(10.8f, 6.3f, 12.25f), new Vector3(3f, 1.8f, 4f), Vector3.zero);

            Lighting(world);

            // The hero, the camera and the test pilot.
            var heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/Hero/Hero.prefab");
            var heroGo = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab, scene);
            heroGo.transform.SetPositionAndRotation(new Vector3(0f, 0f, -3f), Quaternion.identity);
            var hero = heroGo.GetComponent<HeroController>();

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.08f;
            cam.farClipPlane = 400f;
            camGo.AddComponent<AudioListener>();
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camGo.transform.SetPositionAndRotation(new Vector3(0f, 2.4f, -6.3f), Quaternion.Euler(16f, 0f, 0f));
            var orbit = camGo.AddComponent<OrbitCamera>();
            orbit.hero = hero;
            orbit.input = heroGo.GetComponent<HeroInput>();
            hero.cameraTransform = camGo.transform;

            var pilotRoot = new GameObject("Pilot");
            Marker(pilotRoot.transform, "WallBase", new Vector3(-1.85f, 0f, 3.3f));
            Marker(pilotRoot.transform, "WallTop", new Vector3(-1.85f, 4.02f, 3.92f));
            Marker(pilotRoot.transform, "PullUpSpot", new Vector3(1.75f, 4.02f, 3.92f));
            Marker(pilotRoot.transform, "GapRunup", new Vector3(1.6f, 4.02f, 6.0f));
            Marker(pilotRoot.transform, "GapEdge", new Vector3(1.6f, 4.02f, 8.25f));
            Marker(pilotRoot.transform, "GapLanding", new Vector3(1.6f, 4.02f, 11.6f));
            Marker(pilotRoot.transform, "SluiceStand", stand.position);
            Marker(pilotRoot.transform, "CrateEdge", new Vector3(3.15f, 4.02f, 12.2f));
            Marker(pilotRoot.transform, "Crate", new Vector3(6f, 3.95f, 12.2f));
            Marker(pilotRoot.transform, "HighLedge", new Vector3(8.4f, 5.42f, 12.2f));
            Marker(pilotRoot.transform, "Goal", new Vector3(10.8f, 5.42f, 12.25f));
            var pilot = pilotRoot.AddComponent<StretchPilot>();
            pilot.hero = hero;
            pilot.water = waterBody;
            pilot.enabled = false;   // the test run turns it on
            var hud = new GameObject("Hud").AddComponent<StretchHud>();
            hud.hero = hero;
            var recorder = camGo.AddComponent<FrameRecorder>();
            recorder.pilot = pilot;
            recorder.enabled = false;

            Folder("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            return $"built {ScenePath}: {meshes.Count} meshes, {mats.Count} materials";
        }

        // ------------------------------------------------------------------ pieces

        static void Holds(Transform parent, Material brick, Material stone)
        {
            // A readable route up the climbing face: protruding bricks and pale stones,
            // staggered left and right about a line, a hand's reach apart.
            var rng = new System.Random(5);
            int i = 0;
            for (float y = 0.45f; y < 3.75f; y += 0.42f)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = -1.85f + side * (0.32f + (float)rng.NextDouble() * 0.12f) + ((i % 2 == 0) ? 0.1f : -0.1f);
                    bool pale = (i + side) % 3 == 0;
                    float w = pale ? 0.3f : 0.26f, h = pale ? 0.13f : 0.09f;
                    var min = new Vector3(x - w / 2f, y - h / 2f, 3.86f);
                    var max = new Vector3(x + w / 2f, y + h / 2f, 4.0f);
                    Box($"Hold{i}{(side < 0 ? "L" : "R")}", min, max, pale ? stone : brick, parent, collider: false);
                }
                i++;
            }
        }

        static void AddLedge(Transform parent, string name, Vector3 centre, Quaternion rot, float halfLength)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.SetPositionAndRotation(centre, rot);
            var l = go.AddComponent<Ledge>();
            l.localA = new Vector3(-halfLength, 0f, 0f);
            l.localB = new Vector3(halfLength, 0f, 0f);
        }

        static void Zone(Transform parent, string name, ZoneKind kind, Vector3 centre, Vector3 size, Vector3 spawnAt)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = centre;
            var z = go.AddComponent<Zone>();
            z.kind = kind;
            z.size = size;
            if (kind == ZoneKind.Checkpoint)
            {
                var s = new GameObject("Spawn").transform;
                s.SetParent(go.transform);
                s.SetPositionAndRotation(spawnAt, Quaternion.identity);
                z.spawn = s;
            }
        }

        static void Marker(Transform parent, string name, Vector3 at)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent);
            t.position = at;
        }

        static void City(Transform parent, Material adobe, Material light, Material brick, Material stone)
        {
            var city = new GameObject("City").transform;
            city.SetParent(parent);
            var rng = new System.Random(17);
            float R() => (float)rng.NextDouble();
            // Houses in rings beyond the play space, low in front, taller behind.
            for (int k = 0; k < 46; k++)
            {
                float ang = Mathf.Lerp(-150f, 150f, R()) * Mathf.Deg2Rad;
                float dist = 24f + R() * 30f;
                var c = new Vector3(Mathf.Sin(ang) * dist + 3f, 0f, Mathf.Cos(ang) * dist + 6f);
                float w = 3f + R() * 4f, d = 3f + R() * 4f, h = 2.5f + R() * (dist > 38f ? 9f : 5f);
                var m = R() > 0.5f ? adobe : light;
                Box($"House{k}", c + new Vector3(-w / 2f, -0.5f, -d / 2f), c + new Vector3(w / 2f, h, d / 2f), m, city, collider: false);
                Box($"House{k}Cap", c + new Vector3(-w / 2f - 0.08f, h, -d / 2f - 0.08f), c + new Vector3(w / 2f + 0.08f, h + 0.18f, d / 2f + 0.08f), stone, city, collider: false);
            }
            // An aqueduct of brick arches across the back of the view.
            var arch = ArchMesh(3.2f, 1.2f, 1.1f, 0.9f);
            for (int a = 0; a < 14; a++)
            {
                float x = -24f + a * 4.2f;
                Box($"AqueductPier{a}", new Vector3(x - 0.5f, -0.5f, 34f), new Vector3(x + 0.5f, 9f, 35.2f), brick, city, collider: false);
                var go = new GameObject($"AqueductArch{a}");
                go.transform.SetParent(city);
                go.transform.position = new Vector3(x + 2.1f, 9f, 34.6f);
                go.AddComponent<MeshFilter>().sharedMesh = arch;
                go.AddComponent<MeshRenderer>().sharedMaterial = brick;
            }
            Box("AqueductChannel", new Vector3(-24.6f, 10.2f, 33.9f), new Vector3(35.6f, 10.9f, 35.3f), stone, city, collider: false);
        }

        // ------------------------------------------------------------------ lighting

        static void Lighting(Transform parent)
        {
            // The concept video's high sun from the right, warm; shadows fall left and short.
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.transform.SetParent(parent);
            sun.type = LightType.Directional;
            sun.color = Hex("#fff0d8");
            sun.intensity = 2.1f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.85f;
            sun.transform.rotation = Quaternion.LookRotation(new Vector3(-0.55f, -0.78f, 0.32f));
            // A weak warm bounce from the other side keeps shadows brown, never black (SPEC.md).
            var bounce = new GameObject("Bounce").AddComponent<Light>();
            bounce.transform.SetParent(parent);
            bounce.type = LightType.Directional;
            bounce.color = Hex("#e39a62");
            bounce.intensity = 0.35f;
            bounce.shadows = LightShadows.None;
            bounce.transform.rotation = Quaternion.LookRotation(new Vector3(0.6f, 0.25f, -0.4f));

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Hex("#a9cbe3");
            RenderSettings.ambientEquatorColor = Hex("#d9a273");
            RenderSettings.ambientGroundColor = Hex("#9c5a35");
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Hex("#d6e3e6");
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 160f;
            var sky = new Material(Shader.Find("Skybox/Procedural")) { name = "Sky" };
            sky.SetColor("_SkyTint", Hex("#5b97c7"));
            sky.SetColor("_GroundColor", Hex("#d7945d"));
            sky.SetFloat("_AtmosphereThickness", 0.75f);
            sky.SetFloat("_Exposure", 1.25f);
            sky.SetFloat("_SunSize", 0.03f);
            AssetDatabase.DeleteAsset($"{EnvDir}/Materials/Sky.mat");
            AssetDatabase.CreateAsset(sky, $"{EnvDir}/Materials/Sky.mat");
            RenderSettings.skybox = sky;
            RenderSettings.sun = sun;

            var profilePath = $"{EnvDir}/StretchVolume.asset";
            AssetDatabase.DeleteAsset(profilePath);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);
            var grade = profile.Add<ColorAdjustments>(true);
            grade.contrast.Override(8f);
            grade.saturation.Override(10f);
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.25f);
            bloom.threshold.Override(1.1f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.16f);
            foreach (var c in profile.components)
                AssetDatabase.AddObjectToAsset(c, profile);
            EditorUtility.SetDirty(profile);
            var volume = new GameObject("PostVolume").AddComponent<Volume>();
            volume.transform.SetParent(parent);
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        // ------------------------------------------------------------------ building blocks

        static GameObject Box(string name, Vector3 min, Vector3 max, Material mat, Transform parent, bool collider = true)
        {
            var size = max - min;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = (min + max) / 2f;
            go.AddComponent<MeshFilter>().sharedMesh = BoxMesh(size);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            if (collider)
                go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        // A box centred on the origin whose UVs are in metres / TileMeters on every face, so a
        // texture keeps its real size whatever the box's proportions.
        static Mesh BoxMesh(Vector3 s)
        {
            var key = $"box_{s.x:0.000}x{s.y:0.000}x{s.z:0.000}";
            if (meshes.TryGetValue(key, out var cached))
                return cached;
            var h = s / 2f;
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            void Face(Vector3 normal, Vector3 uAxis, Vector3 vAxis, float uLen, float vLen)
            {
                int b = v.Count;
                var c = Vector3.Scale(normal, h);
                var u = uAxis * (uLen / 2f);
                var w = vAxis * (vLen / 2f);
                v.Add(c - u - w); v.Add(c + u - w); v.Add(c + u + w); v.Add(c - u + w);
                for (int i = 0; i < 4; i++) n.Add(normal);
                uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(uLen / TileMeters, 0f));
                uv.Add(new Vector2(uLen / TileMeters, vLen / TileMeters)); uv.Add(new Vector2(0f, vLen / TileMeters));
                tri.Add(b); tri.Add(b + 2); tri.Add(b + 1); tri.Add(b); tri.Add(b + 3); tri.Add(b + 2);
            }
            Face(Vector3.forward, Vector3.left, Vector3.up, s.x, s.y);
            Face(Vector3.back, Vector3.right, Vector3.up, s.x, s.y);
            Face(Vector3.right, Vector3.forward, Vector3.up, s.z, s.y);
            Face(Vector3.left, Vector3.back, Vector3.up, s.z, s.y);
            Face(Vector3.up, Vector3.right, Vector3.forward, s.x, s.z);
            Face(Vector3.down, Vector3.right, Vector3.back, s.x, s.z);
            var mesh = new Mesh { name = key };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            SaveMesh(mesh, key);
            meshes[key] = mesh;
            return mesh;
        }

        // The span between two piers: a slab of width w (centred on x 0), standing on y 0,
        // rising to `rise` above the opening's crown, with a semicircular opening of radius w/2.
        static Mesh ArchMesh(float w, float depth, float rise, float springDrop)
        {
            const int seg = 16;
            float r = w / 2f, top = r + rise;
            var v = new List<Vector3>();
            var tri = new List<int>();
            var uv = new List<Vector2>();
            // Front and back faces: quads between the intrados and the outer outline.
            for (int side = 0; side < 2; side++)
            {
                float z = side == 0 ? -depth / 2f : depth / 2f;
                int b = v.Count;
                for (int i = 0; i <= seg; i++)
                {
                    float a = Mathf.PI * i / seg;
                    var inner = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r - springDrop, z);
                    var outer = new Vector3(Mathf.Clamp(Mathf.Cos(a) * r * 3f, -r, r), top - springDrop, z);
                    if (i == 0) outer = new Vector3(r, top - springDrop, z);
                    if (i == seg) outer = new Vector3(-r, top - springDrop, z);
                    v.Add(inner); v.Add(outer);
                    uv.Add(new Vector2(inner.x, inner.y) / TileMeters); uv.Add(new Vector2(outer.x, outer.y) / TileMeters);
                }
                for (int i = 0; i < seg; i++)
                {
                    int a0 = b + i * 2, a1 = a0 + 2;
                    if (side == 0) { tri.Add(a0); tri.Add(a0 + 1); tri.Add(a1 + 1); tri.Add(a0); tri.Add(a1 + 1); tri.Add(a1); }
                    else { tri.Add(a0); tri.Add(a1 + 1); tri.Add(a0 + 1); tri.Add(a0); tri.Add(a1); tri.Add(a1 + 1); }
                }
            }
            // The soffit: the underside of the arch.
            int s0 = v.Count;
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.PI * i / seg;
                var p = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r - springDrop, 0f);
                v.Add(new Vector3(p.x, p.y, -depth / 2f)); v.Add(new Vector3(p.x, p.y, depth / 2f));
                uv.Add(new Vector2(a * r, 0f) / TileMeters); uv.Add(new Vector2(a * r, depth) / TileMeters);
            }
            for (int i = 0; i < seg; i++)
            {
                int a0 = s0 + i * 2, a1 = a0 + 2;
                tri.Add(a0); tri.Add(a1); tri.Add(a1 + 1); tri.Add(a0); tri.Add(a1 + 1); tri.Add(a0 + 1);
            }
            // The top.
            int t0 = v.Count;
            v.Add(new Vector3(-r, top - springDrop, -depth / 2f)); v.Add(new Vector3(r, top - springDrop, -depth / 2f));
            v.Add(new Vector3(r, top - springDrop, depth / 2f)); v.Add(new Vector3(-r, top - springDrop, depth / 2f));
            uv.Add(Vector2.zero); uv.Add(new Vector2(w, 0f) / TileMeters); uv.Add(new Vector2(w, depth) / TileMeters); uv.Add(new Vector2(0f, depth) / TileMeters);
            tri.Add(t0); tri.Add(t0 + 2); tri.Add(t0 + 1); tri.Add(t0); tri.Add(t0 + 3); tri.Add(t0 + 2);

            var mesh = new Mesh { name = "arch" };
            mesh.SetVertices(v);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            SaveMesh(mesh, "arch");
            meshes["arch"] = mesh;
            return mesh;
        }

        static void SaveMesh(Mesh mesh, string key)
        {
            var path = $"{EnvDir}/Meshes/{key}.asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old != null)
            {
                EditorUtility.CopySerialized(mesh, old);
                return;
            }
            AssetDatabase.CreateAsset(mesh, path);
        }

        static Material Mat(string name, Color color, Texture2D tex)
        {
            var path = $"{EnvDir}/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            m.SetTexture("_BaseMap", tex);
            m.SetFloat("_Smoothness", 0.12f);
            m.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(m);
            mats[name] = m;
            return m;
        }

        static Material WaterMat()
        {
            var path = $"{EnvDir}/Materials/Water.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Water" };
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetFloat("_Surface", 1f);   // transparent
            m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.renderQueue = (int)RenderQueue.Transparent;
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            var c = Hex("#6fcbb5");
            c.a = 0.82f;
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", 0.85f);
            EditorUtility.SetDirty(m);
            mats["Water"] = m;
            return m;
        }

        // A tileable greyscale "painted" texture: soft blotches of periodic value noise, with
        // strokes stretched along u, centred on white so the material colour carries the hue.
        static Texture2D PaintedTexture(string name, int seed, float stroke, float strength)
        {
            const int size = 512;
            var path = $"{EnvDir}/Textures/{name}.png";
            var rng = new System.Random(seed);
            // Periodic value noise: a lattice of cu x cv cells that wraps exactly once across
            // the texture, so the texture tiles.
            float Noise(float[] lat, int cu, int cv, float x, float y)
            {
                int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
                float fx = x - x0, fy = y - y0;
                fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
                float L(int i, int j) => lat[((j % cv + cv) % cv) * cu + ((i % cu + cu) % cu)];
                return Mathf.Lerp(Mathf.Lerp(L(x0, y0), L(x0 + 1, y0), fx), Mathf.Lerp(L(x0, y0 + 1), L(x0 + 1, y0 + 1), fx), fy);
            }
            var octaves = new[] { (4, 0.5f), (8, 0.27f), (16, 0.15f), (32, 0.08f) };
            var lats = new float[octaves.Length][];
            var cus = new int[octaves.Length];
            for (int o = 0; o < octaves.Length; o++)
            {
                int cv = octaves[o].Item1;
                // Strokes: fewer cells along u than along v, so the noise runs longer along u.
                cus[o] = Mathf.Max(1, Mathf.RoundToInt(cv / stroke));
                lats[o] = new float[cus[o] * cv];
                for (int i = 0; i < lats[o].Length; i++) lats[o][i] = (float)rng.NextDouble();
            }
            var tex = new Texture2D(size, size, TextureFormat.RGB24, true);
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float u = (float)x / size, v = (float)y / size, sum = 0f;
                    for (int o = 0; o < octaves.Length; o++)
                    {
                        int cv = octaves[o].Item1, cu = cus[o];
                        sum += Noise(lats[o], cu, cv, u * cu, v * cv) * octaves[o].Item2;
                    }
                    float g = 1f + (sum - 0.5f) * 2f * strength;
                    px[y * size + x] = new Color(g, g, g);
                }
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var ti = (TextureImporter)AssetImporter.GetAtPath(path);
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.sRGBTexture = true;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            Folder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
