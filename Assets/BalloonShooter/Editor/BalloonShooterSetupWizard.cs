using System.IO;
using BalloonShooter;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BalloonShooter.Editor
{
    public static class BalloonShooterSetupWizard
    {
        private const string Root = "Assets/BalloonShooter";
        private const string MaterialsDir = Root + "/Materials";
        private const string ScriptableObjectsDir = Root + "/ScriptableObjects";
        private const string ScenesDir = Root + "/Scenes";

        private const string ScenePath = ScenesDir + "/BalloonShooter.unity";
        private const string ConfigPath = ScriptableObjectsDir + "/GameConfig_Default.asset";

        public static string GeneratedScenePath => ScenePath;
        public static string GeneratedConfigPath => ConfigPath;

        [MenuItem("Balloon Shooter/Setup or Update Game")]
        public static void SetupOrUpdateGame()
        {
            EnsureFolders();

            Material latex = EnsureMaterial("Mat_Latex", new Color(1f, 1f, 1f, 1f), metallic: 0.0f, smoothness: 0.65f, transparent: false);
            Material metal = EnsureMaterial("Mat_Metallic", new Color(1f, 1f, 1f, 1f), metallic: 0.9f, smoothness: 0.9f, transparent: false);
            Material glass = EnsureMaterial("Mat_Glass", new Color(1f, 1f, 1f, 0.35f), metallic: 0.0f, smoothness: 0.95f, transparent: true);
            Material bomb = EnsureMaterial("Mat_Bomb", new Color(0.25f, 0.05f, 0.05f, 1f), metallic: 0.0f, smoothness: 0.75f, transparent: false);
            Material gold = EnsureMaterial("Mat_Golden", new Color(1f, 0.82f, 0.2f, 1f), metallic: 1.0f, smoothness: 0.95f, transparent: false);

            BalloonType normalType = EnsureBalloonType("BalloonType_Normal", "Latex", BalloonSpecial.Normal, latex, basePoints: 50f, hp: 1, rarity: 8f);
            BalloonType metallicType = EnsureBalloonType("BalloonType_Metallic", "Metallic", BalloonSpecial.Metallic, metal, basePoints: 90f, hp: 2, rarity: 2.2f);
            BalloonType glassType = EnsureBalloonType("BalloonType_Glass", "Glass", BalloonSpecial.Glass, glass, basePoints: 110f, hp: 1, rarity: 1.8f);
            BalloonType bombType = EnsureBalloonType("BalloonType_Bomb", "Bomb", BalloonSpecial.Bomb, bomb, basePoints: 70f, hp: 1, rarity: 1.5f);
            BalloonType splitterType = EnsureBalloonType("BalloonType_Splitter", "Splitter", BalloonSpecial.Splitter, latex, basePoints: 85f, hp: 1, rarity: 1.6f);
            BalloonType goldenType = EnsureBalloonType("BalloonType_Golden", "Golden", BalloonSpecial.Golden, gold, basePoints: 250f, hp: 1, rarity: 0.35f);

            GameConfig config = EnsureGameConfig();
            config.balloonTypes.Clear();
            config.balloonTypes.Add(normalType);
            config.balloonTypes.Add(metallicType);
            config.balloonTypes.Add(glassType);
            config.balloonTypes.Add(bombType);
            config.balloonTypes.Add(splitterType);
            config.balloonTypes.Add(goldenType);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            EnsureScene(config);
            EnsureSceneInBuildSettings(ScenePath);
            Debug.Log("Balloon Shooter: setup complete.");
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(MaterialsDir);
            Directory.CreateDirectory(ScriptableObjectsDir);
            Directory.CreateDirectory(ScenesDir);
            AssetDatabase.Refresh();
        }

        private static Material EnsureMaterial(string name, Color color, float metallic, float smoothness, bool transparent)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);

            Shader shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            if (material == null)
            {
                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.color = color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            ApplyStandardRenderingMode(material, transparent);

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ApplyStandardRenderingMode(Material material, bool transparent)
        {
            if (material == null) return;
            if (material.shader == null || material.shader.name != "Standard") return;

            if (transparent)
            {
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetFloat("_Mode", 3f);
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                material.SetOverrideTag("RenderType", "Opaque");
                material.SetFloat("_Mode", 0f);
                material.SetInt("_SrcBlend", (int)BlendMode.One);
                material.SetInt("_DstBlend", (int)BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
            }
        }

        private static BalloonType EnsureBalloonType(string assetName, string displayName, BalloonSpecial special, Material material, float basePoints, int hp, float rarity)
        {
            string path = $"{ScriptableObjectsDir}/{assetName}.asset";
            var type = AssetDatabase.LoadAssetAtPath<BalloonType>(path);
            if (type == null)
            {
                type = ScriptableObject.CreateInstance<BalloonType>();
                AssetDatabase.CreateAsset(type, path);
            }

            type.displayName = displayName;
            type.special = special;
            type.material = material;
            type.basePoints = basePoints;
            type.hitPoints = hp;
            type.rarityWeight = rarity;

            // Larger balloons for better readability at 4K.
            type.radiusRange = special == BalloonSpecial.Golden ? new Vector2(0.5f, 0.85f) : new Vector2(0.35f, 0.95f);
            type.ascentSpeedRange = special == BalloonSpecial.Glass ? new Vector2(0.75f, 1.55f) : new Vector2(0.55f, 1.35f);
            type.lateralSpeedRange = new Vector2(0.0f, 0.85f);
            type.bombRadius = 2.75f;
            type.chainPopPointMultiplier = 0.55f;
            type.splitterChildCount = 2;
            type.goldenTimeBonusSeconds = 3.5f;

            type.colorGradient = CreateDefaultGradient(special);
            EditorUtility.SetDirty(type);
            return type;
        }

        private static Gradient CreateDefaultGradient(BalloonSpecial special)
        {
            Color a;
            Color b;

            switch (special)
            {
                case BalloonSpecial.Metallic:
                    a = new Color(0.85f, 0.9f, 1f);
                    b = new Color(1f, 0.7f, 0.9f);
                    break;
                case BalloonSpecial.Glass:
                    a = new Color(0.7f, 0.9f, 1f, 0.35f);
                    b = new Color(0.9f, 1f, 0.8f, 0.35f);
                    break;
                case BalloonSpecial.Bomb:
                    a = new Color(0.9f, 0.15f, 0.2f);
                    b = new Color(0.3f, 0.05f, 0.05f);
                    break;
                case BalloonSpecial.Golden:
                    a = new Color(1f, 0.85f, 0.2f);
                    b = new Color(1f, 0.65f, 0.1f);
                    break;
                default:
                    a = new Color(0.35f, 0.8f, 0.35f);
                    b = new Color(0.25f, 0.55f, 0.95f);
                    break;
            }

            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(a, 0f),
                    new GradientColorKey(b, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(a.a, 0f),
                    new GradientAlphaKey(b.a, 1f),
                }
            );
            return g;
        }

        private static GameConfig EnsureGameConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            config.gameDurationSeconds = 20f;
            config.maxAliveBalloons = 24;
            config.spawnDistanceRange = new Vector2(10f, 28f);
            // With "falling balloons" the spawn band should be biased above the camera.
            config.spawnHeightRange = new Vector2(1.5f, 5.0f);
            config.spawnHorizontalHalfWidth = 8.5f;
            config.despawnHeight = 9.5f;
            config.windChangeIntervalSeconds = 2.4f;
            config.windStrength = 0.75f;
            config.comboWindowSeconds = 1.25f;
            config.comboMultiplierStep = 0.15f;
            config.comboMultiplierMax = 3f;
            config.missComboPenalty = 999;

            config.enableBalloonDashing = true;
            config.dashTargetRange = 4.5f;
            config.dashSpeed = 4.0f;
            config.dashDurationSeconds = 0.2f;
            config.dashIntervalSecondsRange = new Vector2(1.25f, 2.5f);

            config.spawnRateOverTime = new AnimationCurve(
                new Keyframe(0f, 1.0f),
                new Keyframe(0.45f, 1.55f),
                new Keyframe(1f, 2.4f)
            );

            EditorUtility.SetDirty(config);
            return config;
        }

        private static void EnsureScene(GameConfig config)
        {
            if (File.Exists(ScenePath))
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            else
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera cam = Object.FindFirstObjectByType<Camera>();
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.orthographic = false;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            cam.clearFlags = CameraClearFlags.Skybox;

            Shooter shooter = cam.GetComponent<Shooter>();
            if (shooter == null) shooter = cam.gameObject.AddComponent<Shooter>();

            if (Object.FindFirstObjectByType<Light>() == null)
            {
                var lightGo = new GameObject("Directional Light");
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.0f;
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            // 2D setup: no 3D floor needed.
            var floor = GameObject.Find("Floor");
            if (floor != null) Object.DestroyImmediate(floor);

            var systems = GameObject.Find("Systems");
            if (systems == null) systems = new GameObject("Systems");

            var score = systems.GetComponent<ScoreManager>();
            if (score == null) score = systems.AddComponent<ScoreManager>();

            var spawnerGo = GameObject.Find("BalloonSpawner");
            if (spawnerGo == null) spawnerGo = new GameObject("BalloonSpawner");

            var balloonParent = GameObject.Find("Balloons");
            if (balloonParent == null) balloonParent = new GameObject("Balloons");

            var spawner = spawnerGo.GetComponent<BalloonSpawner>();
            if (spawner == null) spawner = spawnerGo.AddComponent<BalloonSpawner>();

            var manager = systems.GetComponent<GameManager>();
            if (manager == null) manager = systems.AddComponent<GameManager>();

            SetupReferences(manager, config, spawner, shooter, score);
            SetupSpawnerReferences(spawner, config, cam, balloonParent.transform);

            var hud = EnsureHud(manager, score);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), ScenePath);

            Selection.activeObject = hud != null ? hud.gameObject : systems;
        }

        private static void EnsureSceneInBuildSettings(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    scenes[i].enabled = true;
                    EditorBuildSettings.scenes = scenes;
                    return;
                }
            }

            var list = new EditorBuildSettingsScene[scenes.Length + 1];
            for (int i = 0; i < scenes.Length; i++) list[i] = scenes[i];
            list[list.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = list;
        }

        private static void EnsureFloor()
        {
            var floor = GameObject.Find("Floor");
            if (floor == null)
            {
                floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
                floor.name = "Floor";
            }

            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(5f, 1f, 7f);

            var renderer = floor.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = renderer.sharedMaterial;
                if (mat != null && mat.HasProperty("_Color"))
                    mat.color = new Color(0.22f, 0.24f, 0.28f, 1f);
            }
        }

        private static void SetupReferences(GameManager manager, GameConfig config, BalloonSpawner spawner, Shooter shooter, ScoreManager score)
        {
            var so = new SerializedObject(manager);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("spawner").objectReferenceValue = spawner;
            so.FindProperty("shooter").objectReferenceValue = shooter;
            so.FindProperty("scoreManager").objectReferenceValue = score;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetupSpawnerReferences(BalloonSpawner spawner, GameConfig config, Camera cam, Transform balloonParent)
        {
            var so = new SerializedObject(spawner);
            so.FindProperty("config").objectReferenceValue = config;
            so.FindProperty("spawnCamera").objectReferenceValue = cam;
            so.FindProperty("balloonParent").objectReferenceValue = balloonParent;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static HUDController EnsureHud(GameManager manager, ScoreManager score)
        {
            var canvasGo = GameObject.Find("HUD");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("HUD");
                int uiLayer = LayerMask.NameToLayer("UI");
                if (uiLayer >= 0) canvasGo.layer = uiLayer;
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                Apply4kCanvasScalerDefaults(scaler);
                canvasGo.AddComponent<GraphicRaycaster>();
            }
            else
            {
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                if (scaler != null) Apply4kCanvasScalerDefaults(scaler);
            }

            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            var hud = canvasGo.GetComponent<HUDController>();
            if (hud == null) hud = canvasGo.AddComponent<HUDController>();

            TMP_Text scoreText = EnsureText(canvasGo.transform, "ScoreText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -12f), 48, TextAlignmentOptions.TopLeft);
            TMP_Text highScoreText = EnsureText(canvasGo.transform, "HighScoreText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -64f), 34, TextAlignmentOptions.TopLeft);
            TMP_Text timeText = EnsureText(canvasGo.transform, "TimeText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -104f), 36, TextAlignmentOptions.TopLeft);
            TMP_Text comboText = EnsureText(canvasGo.transform, "ComboText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -152f), 32, TextAlignmentOptions.TopLeft);
            TMP_Text accuracyText = EnsureText(canvasGo.transform, "AccuracyText", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -196f), 30, TextAlignmentOptions.TopLeft);

            TMP_Text crosshair = EnsureText(canvasGo.transform, "Crosshair", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), 84, TextAlignmentOptions.Center);
            crosshair.text = "+";

            var endPanelGo = GameObject.Find("EndPanel");
            if (endPanelGo == null)
            {
                endPanelGo = new GameObject("EndPanel");
                endPanelGo.transform.SetParent(canvasGo.transform, false);
                var rect = endPanelGo.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                var img = endPanelGo.AddComponent<Image>();
                img.color = new Color(0f, 0f, 0f, 0.6f);
            }

            var endGroup = endPanelGo.GetComponent<CanvasGroup>();
            if (endGroup == null) endGroup = endPanelGo.AddComponent<CanvasGroup>();

            TMP_Text endText = EnsureText(endPanelGo.transform, "EndSummaryText", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), 64, TextAlignmentOptions.Center);

            var so = new SerializedObject(hud);
            so.FindProperty("manager").objectReferenceValue = manager;
            so.FindProperty("score").objectReferenceValue = score;
            so.FindProperty("scoreText").objectReferenceValue = scoreText;
            so.FindProperty("highScoreText").objectReferenceValue = highScoreText;
            so.FindProperty("timeText").objectReferenceValue = timeText;
            so.FindProperty("comboText").objectReferenceValue = comboText;
            so.FindProperty("accuracyText").objectReferenceValue = accuracyText;
            so.FindProperty("endPanel").objectReferenceValue = endGroup;
            so.FindProperty("endSummaryText").objectReferenceValue = endText;
            so.ApplyModifiedPropertiesWithoutUndo();

            return hud;
        }

        private static TMP_Text EnsureText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, int fontSize, TextAlignmentOptions alignment)
        {
            Transform existing = parent.Find(name);
            GameObject go = existing != null ? existing.gameObject : new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            if (rect == null) rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin == anchorMax ? anchorMin : new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(1400f, 220f);

            var text = go.GetComponent<TextMeshProUGUI>();
            if (text == null) text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.enableWordWrapping = false;
            text.color = Color.white;

            if (string.IsNullOrWhiteSpace(text.text))
                text.text = name;

            return text;
        }

        private static void Apply4kCanvasScalerDefaults(CanvasScaler scaler)
        {
            if (scaler == null) return;

            // Use a 1080p reference so UI scales up on 4K displays (4K => ~2.0 scale).
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
        }
    }
}
