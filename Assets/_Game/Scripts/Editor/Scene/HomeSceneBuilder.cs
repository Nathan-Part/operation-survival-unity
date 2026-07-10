using System.Collections.Generic;
using Game.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Runtime.Editor
{
    /// Scène d'accueil (parité web lobby, main.js:497-557) : perso 3D + fond `lobby-bg.jpg`,
    /// caméra fixe (position/lookAt relevées dans `setLobbyCamera`, main.js:554-557), UI Toolkit
    /// par-dessus (HomeController). Régénérable à volonté (Tools → Game → Build Home Scene).
    public static class HomeSceneBuilder
    {
        const string ScenePath = "Assets/_Game/Scenes/Home.unity";
        const string DatabasePath = "Assets/_Game/Data/GameDatabase.asset";
        const string BackgroundPath = "Assets/_Game/Art/UI/background.png"; // playtest Phase 5 (2026-07-09) : lobby-bg.jpg n'etait pas le bon fond, background.png (fourni par Nathan) est identique a l'asset deja copie pour la vignette map-utopia
        const string PanelSettingsPath = "Assets/_Game/UI/Home/HomePanelSettings.asset";
        const string UxmlPath = "Assets/_Game/UI/Home/Home.uxml";
        const string SettingsPanelUxmlPath = "Assets/_Game/UI/Home/SettingsPanel.uxml";
        const string MapPanelUxmlPath = "Assets/_Game/UI/Home/MapPanel.uxml";
        const string LoadoutPanelUxmlPath = "Assets/_Game/UI/Home/LoadoutPanel.uxml";
        const string ShopPanelUxmlPath = "Assets/_Game/UI/Home/ShopPanel.uxml";
        const string HowToPanelUxmlPath = "Assets/_Game/UI/Home/HowToPanel.uxml";

        [MenuItem("Tools/Game/Build Home Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            // web: setLobbyCamera (main.js:554-557) — camera.position.set(0, 1.95, 6.8); camera.lookAt(0, 1.46, 0);
            camGo.transform.position = new Vector3(0f, 1.95f, 6.8f);
            camGo.transform.LookAt(new Vector3(0f, 1.46f, 0f));

            BuildBackground();

            var contextGo = new GameObject("GameContext");
            var context = contextGo.AddComponent<GameContext>();
            context.database = AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if (context.database == null)
                Debug.LogError($"HomeSceneBuilder: GameDatabase introuvable a {DatabasePath}");

            var metaGo = new GameObject("MetaProgress");
            var meta = metaGo.AddComponent<MetaProgress>();
            // Pas de LoadOrCreate() ici : MetaProgress.Data n'est jamais sérialisé (propriété
            // runtime pure) - un chargement fait à l'édition serait perdu au Play Mode suivant.
            // HomeController.WireUi() charge la save au vrai démarrage (Start()).

            var stageGo = new GameObject("LobbyCharacterStage");
            var stage = stageGo.AddComponent<LobbyCharacterStage>();
            stage.Init(context.database);

            var uiGo = new GameObject("UIDocument");
            var document = uiGo.AddComponent<UIDocument>();
            document.panelSettings = LoadOrCreatePanelSettings();
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (document.visualTreeAsset == null)
                Debug.LogError($"HomeSceneBuilder: Home.uxml introuvable a {UxmlPath}");

            // GameLauncher partage le GameObject UIDocument : sa propre requête #boot-loading
            // se fait aussi dans son Start() (même raison — cf. commentaire sur GameLauncher).
            var launcher = uiGo.AddComponent<GameLauncher>();

            // SettingsPanelController partage aussi le GameObject UIDocument : clone SettingsPanel.uxml
            // dans le conteneur vide #settings-panel au Play Mode (Task 8) — jamais à l'édition (Init()
            // seul ici, câblage réel en Start(), même raison que les autres contrôleurs de cette scène).
            var settingsPanel = uiGo.AddComponent<SettingsPanelController>();
            var settingsPanelAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SettingsPanelUxmlPath);
            if (settingsPanelAsset == null)
                Debug.LogError($"HomeSceneBuilder: SettingsPanel.uxml introuvable a {SettingsPanelUxmlPath}");

            // MapPanelController partage aussi le GameObject UIDocument : clone MapPanel.uxml dans
            // le conteneur vide #map-panel au Play Mode (Task 9) — même raison que SettingsPanelController.
            var mapPanel = uiGo.AddComponent<MapPanelController>();
            var mapPanelAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(MapPanelUxmlPath);
            if (mapPanelAsset == null)
                Debug.LogError($"HomeSceneBuilder: MapPanel.uxml introuvable a {MapPanelUxmlPath}");

            // LoadoutPanelController partage aussi le GameObject UIDocument : clone LoadoutPanel.uxml
            // dans le conteneur vide #loadout-panel au Play Mode (Task 10) — même raison que MapPanelController.
            var loadoutPanel = uiGo.AddComponent<LoadoutPanelController>();
            var loadoutPanelAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LoadoutPanelUxmlPath);
            if (loadoutPanelAsset == null)
                Debug.LogError($"HomeSceneBuilder: LoadoutPanel.uxml introuvable a {LoadoutPanelUxmlPath}");

            // ShopScreenController partage aussi le GameObject UIDocument : clone ShopPanel.uxml
            // dans le conteneur vide #shop-panel au Play Mode (Task 11) — même raison que les autres
            // contrôleurs de panneau. Contrairement à Map/Loadout/Settings, ne reçoit PAS de référence
            // à HomeController (voir commentaire sur ShopScreenController) : son bouton "Fermer" est
            // autonome, HomeController tient la référence inverse pour router Échap.
            var shopScreen = uiGo.AddComponent<ShopScreenController>();
            var shopPanelAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ShopPanelUxmlPath);
            if (shopPanelAsset == null)
                Debug.LogError($"HomeSceneBuilder: ShopPanel.uxml introuvable a {ShopPanelUxmlPath}");

            // HowToPanelController partage aussi le GameObject UIDocument : clone HowToPanel.uxml
            // dans le conteneur vide #howto-panel au Play Mode (Task 15) — même raison que Map/
            // Loadout/Settings. Purement informatif (pas de SaveData/MetaProgress à lui passer).
            var howToPanel = uiGo.AddComponent<HowToPanelController>();
            var howToPanelAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(HowToPanelUxmlPath);
            if (howToPanelAsset == null)
                Debug.LogError($"HomeSceneBuilder: HowToPanel.uxml introuvable a {HowToPanelUxmlPath}");

            var controller = uiGo.AddComponent<HomeController>();
            // Init() ne fait qu'assigner les références sérialisées (voir commentaire sur la
            // méthode) : le câblage réel (rendu du lobby, perso 3D affiché) se fait au Play Mode
            // via HomeController.Start(), jamais ici (pas de GameObject runtime à sérialiser dans la scène).
            controller.Init(context.database, meta, stage, launcher, settingsPanel, mapPanel, loadoutPanel, shopScreen);
            settingsPanel.Init(meta, controller, settingsPanelAsset);
            mapPanel.Init(meta, controller, mapPanelAsset);
            loadoutPanel.Init(context.database, meta, controller, loadoutPanelAsset);
            shopScreen.Init(context.database, meta, stage, shopPanelAsset);
            howToPanel.Init(controller, howToPanelAsset);

            System.IO.Directory.CreateDirectory("Assets/_Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterHomeAsFirstScene();
            Debug.Log($"HomeSceneBuilder: scene sauvegardee -> {ScenePath}");
        }

        static void BuildBackground()
        {
            var bgTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "LobbyBackground";
            var collider = quad.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider); // décor pur, pas d'obstacle physique
            quad.transform.position = new Vector3(0f, 1.46f, -3f); // derrière le perso, face à la caméra lobby
            // Le Quad primitif Unity a sa face avant orientée (0,0,-1) (vérifié via execute_code) : avec
            // Quaternion.identity la caméra (z=6.8, regarde vers -Z) ne voit que la face arrière (culled,
            // donc fond invisible — c'était la vraie cause du bug A1). Rotation 180° sur Y pour la retourner face caméra.
            quad.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            quad.transform.localScale = new Vector3(12f, 7f, 1f); // taille d'edition ; corrigee au runtime par LobbyBackgroundFitter (frustum reel mesure ~20.1x11.3, cf. commit playtest 2026-07-10)
            quad.AddComponent<Game.Runtime.LobbyBackgroundFitter>();

            if (bgTexture != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
                var material = new Material(shader) { mainTexture = bgTexture };
                quad.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
            else
            {
                Debug.LogWarning($"HomeSceneBuilder: fond introuvable a {BackgroundPath} - quad sans texture.");
            }
        }

        static PanelSettings LoadOrCreatePanelSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (settings != null) return settings;

            settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            // Shrink : la référence 1920×1080 tient toujours entière (rien n'est jamais
            // coupé, quel que soit l'aspect ratio — playtest 2026-07-10 point 4).
            settings.screenMatchMode = PanelScreenMatchMode.Shrink;
            AssetDatabase.CreateAsset(settings, PanelSettingsPath);
            return settings;
        }

        /// Enregistre Home dans les Build Settings et la place en index 0 (premier écran au lancement).
        static void RegisterHomeAsFirstScene()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
