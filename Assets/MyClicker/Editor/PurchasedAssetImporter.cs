using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MyClicker.Editor
{
    /// <summary>
    /// Downloads / imports the five purchased packs, flips the project to portrait iOS,
    /// and creates the first-run + battle scenes. Resumes across domain reloads.
    /// </summary>
    [InitializeOnLoad]
    public static class PurchasedAssetImporter
    {
        const string LogPath = "Assets/MyClicker/Editor/import-log.txt";
        const string StatePath = "Assets/MyClicker/Editor/import-state.json";
        const string DonePath = "Assets/MyClicker/Editor/bootstrap.done";

        const string StonePackage = @"C:\Users\Administrator\AppData\Roaming\Unity\Asset Store-5.x\LAYERLAB\Textures MaterialsGUI Skins\GUI - The Stone.unitypackage";

        static readonly AssetSpec[] Specs =
        {
            new AssetSpec("116852", "GUI The Stone", new[] { "gui - the stone", "the stone" }, new[] { "Layer Lab", "LayerLab", "The Stone", "GUI - The Stone" }),
            new AssetSpec("72935", "Miniature Army 2D V1", new[] { "miniature army 2d v.1", "miniature army 2d v1" }, new[] { "Miniature Army", "MiniatureArmy" }),
            new AssetSpec("71334", "2D Casual Background HD V1", new[] { "2d casual background hd v.1", "2d casual background hd v1" }, new[] { "Casual backgorund", "Casual Background", "CasualBackground" }),
            new AssetSpec("199567", "2D Potion Icon Pack", new[] { "2d potion icon pack" }, new[] { "Potion Icon", "PotionIcon" }),
            new AssetSpec("90592", "Character Maker [Fantasy]", new[] { "character maker [fantasy]" }, new[] { "HeroEditor", "Character Maker", "CharacterMaker" }),
        };

        static UnityEngine.Networking.UnityWebRequest _activeRequest;
        static Action<bool, byte[], string> _activeCallback;

        static PurchasedAssetImporter()
        {
            if (File.Exists(DonePath))
                return;
            EditorApplication.delayCall += Tick;
        }

        [MenuItem("MyClicker/Run Asset Import + iOS Bootstrap")]
        public static void RunManually()
        {
            if (File.Exists(DonePath))
                File.Delete(DonePath);
            SaveState(new ImportState());
            Tick();
        }

        static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += Tick;
                return;
            }

            try
            {
                Advance();
            }
            catch (Exception ex)
            {
                Log("ERROR " + ex);
                EditorApplication.delayCall += Tick;
            }
        }

        static void Advance()
        {
            var state = LoadState();
            EnsureFolders();

            if (!state.stoneQueued)
            {
                if (File.Exists(StonePackage) && !IsPresent(Specs[0]))
                {
                    Log("Importing local GUI The Stone package...");
                    state.stoneQueued = true;
                    SaveState(state);
                    AssetDatabase.ImportPackage(StonePackage, false);
                    return;
                }

                state.stoneQueued = true;
                SaveState(state);
            }

            foreach (var spec in Specs)
            {
                if (state.imported.Contains(spec.id))
                    continue;
                if (IsPresent(spec))
                {
                    Log("Already in project: " + spec.label);
                    state.imported.Add(spec.id);
                    SaveState(state);
                    continue;
                }

                if (state.downloaded.Contains(spec.id))
                {
                    var path = FindDownloadedPackage(spec);
                    if (!string.IsNullOrEmpty(path))
                    {
                        Log("Importing downloaded package " + spec.label + " from " + path);
                        state.imported.Add(spec.id);
                        SaveState(state);
                        AssetDatabase.ImportPackage(path, false);
                        return;
                    }
                }

                if (!state.downloadStarted.Contains(spec.id))
                {
                    state.downloadStarted.Add(spec.id);
                    SaveState(state);
                    if (TryStartInternalDownload(spec))
                    {
                        Log("Started Unity-internal download for " + spec.label);
                        EditorApplication.update -= WatchDownloads;
                        EditorApplication.update += WatchDownloads;
                        return;
                    }

                    StartHttpDownload(spec, state);
                    return;
                }
            }

            if (Specs.Any(s => !IsPresent(s) && !state.imported.Contains(s.id)))
            {
                Log("Waiting for remaining assets...");
                EditorApplication.delayCall += Tick;
                return;
            }

            if (!state.iosConfigured)
            {
                ConfigureIos();
                state.iosConfigured = true;
                SaveState(state);
            }

            if (!state.scenesCreated)
            {
                CreateGameContent();
                state.scenesCreated = true;
                SaveState(state);
            }

            File.WriteAllText(DonePath, DateTime.Now.ToString("o"));
            AssetDatabase.Refresh();
            Log("Bootstrap complete.");
        }

        static void WatchDownloads()
        {
            var state = LoadState();
            bool anyPending = false;
            foreach (var spec in Specs)
            {
                if (IsPresent(spec) || state.imported.Contains(spec.id))
                    continue;
                var path = FindDownloadedPackage(spec);
                if (!string.IsNullOrEmpty(path))
                {
                    EditorApplication.update -= WatchDownloads;
                    state.downloaded.Add(spec.id);
                    SaveState(state);
                    Log("Download finished on disk: " + spec.label + " -> " + path);
                    EditorApplication.delayCall += Tick;
                    return;
                }

                anyPending = true;
            }

            if (!anyPending)
                EditorApplication.update -= WatchDownloads;
        }

        static bool TryStartInternalDownload(AssetSpec spec)
        {
            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type managerType = assembly.GetType("UnityEditor.PackageManager.UI.Internal.AssetStoreDownloadManager")
                                       ?? assembly.GetType("UnityEditor.PackageManager.UI.Internal.AssetStoreDownloadManagerV2");
                    if (managerType == null)
                        continue;

                    object instance = GetSingleton(managerType);
                    if (instance == null)
                        continue;

                    foreach (var name in new[] { "Download", "DownloadNew", "StartDownload" })
                    {
                        foreach (var method in managerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                        {
                            if (method.Name != name)
                                continue;
                            var args = method.GetParameters();
                            if (args.Length == 1 && (args[0].ParameterType == typeof(string) || args[0].ParameterType == typeof(long) || args[0].ParameterType == typeof(int)))
                            {
                                object id = args[0].ParameterType == typeof(string) ? spec.id : (object)int.Parse(spec.id);
                                method.Invoke(instance, new[] { id });
                                return true;
                            }

                            if (args.Length == 2 && args[0].ParameterType == typeof(string))
                            {
                                method.Invoke(instance, new object[] { spec.id, null });
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Internal download failed for " + spec.label + ": " + ex.Message);
            }

            return false;
        }

        static object GetSingleton(Type type)
        {
            foreach (var name in new[] { "instance", "Instance" })
            {
                var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (prop != null)
                    return prop.GetValue(null);
                var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                if (field != null)
                    return field.GetValue(null);
            }

            return null;
        }

        static void StartHttpDownload(AssetSpec spec, ImportState state)
        {
            string token = GetAccessToken();
            if (string.IsNullOrEmpty(token))
            {
                Log("No Unity access token. Open Unity while logged in, then rerun MyClicker/Run Asset Import + iOS Bootstrap.");
                EditorApplication.delayCall += Tick;
                return;
            }

            string infoUrl = "https://packages-v2.unity.com/-/api/legacy-package-download-info/" + spec.id;
            Log("Requesting download info for " + spec.label);
            Get(infoUrl, token, (ok, body, err) =>
            {
                if (!ok)
                {
                    Log("Download-info failed for " + spec.label + ": " + err);
                    EditorApplication.delayCall += Tick;
                    return;
                }

                string json = Encoding.UTF8.GetString(body);
                File.WriteAllText("Assets/MyClicker/Editor/download-info-" + spec.id + ".json", json);
                string url = ExtractJsonString(json, "url");
                if (string.IsNullOrEmpty(url))
                    url = ExtractJsonString(json, "download_url");
                if (string.IsNullOrEmpty(url))
                {
                    Log("No URL in download-info for " + spec.label + ". Body saved.");
                    EditorApplication.delayCall += Tick;
                    return;
                }

                Log("Downloading " + spec.label + " ...");
                Get(url, token, (ok2, bytes, err2) =>
                {
                    if (!ok2)
                    {
                        Log("Download failed for " + spec.label + ": " + err2);
                        EditorApplication.delayCall += Tick;
                        return;
                    }

                    string destDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "Unity", "Asset Store-5.x", "MyClickerDownloads");
                    Directory.CreateDirectory(destDir);
                    string dest = Path.Combine(destDir, spec.id + " " + Sanitize(spec.label) + ".unitypackage");
                    File.WriteAllBytes(dest, bytes);
                    Log("Wrote " + dest + " (" + bytes.Length + " bytes)");
                    var s = LoadState();
                    s.downloaded.Add(spec.id);
                    SaveState(s);
                    EditorApplication.delayCall += Tick;
                });
            });
        }

        static void Get(string url, string token, Action<bool, byte[], string> callback)
        {
            var req = UnityEngine.Networking.UnityWebRequest.Get(url);
            req.timeout = 300;
            if (!string.IsNullOrEmpty(token))
                req.SetRequestHeader("Authorization", "Bearer " + token);
            req.SetRequestHeader("X-Requested-With", "UnityAssetStore");
            req.SendWebRequest();
            _activeRequest = req;
            _activeCallback = callback;
            EditorApplication.update -= PumpRequest;
            EditorApplication.update += PumpRequest;
        }

        static void PumpRequest()
        {
            if (_activeRequest == null || !_activeRequest.isDone)
                return;

            EditorApplication.update -= PumpRequest;
            var req = _activeRequest;
            var cb = _activeCallback;
            _activeRequest = null;
            _activeCallback = null;

            bool ok = req.result == UnityEngine.Networking.UnityWebRequest.Result.Success;
            cb?.Invoke(ok, ok ? req.downloadHandler.data : null, req.error + " " + req.responseCode);
            req.Dispose();
        }

        static string GetAccessToken()
        {
            try
            {
                var prop = typeof(CloudProjectSettings).GetProperty("accessToken", BindingFlags.Public | BindingFlags.Static);
                if (prop != null)
                {
                    string token = prop.GetValue(null) as string;
                    if (!string.IsNullOrEmpty(token))
                        return token;
                }
            }
            catch (Exception ex)
            {
                Log("CloudProjectSettings.accessToken: " + ex.Message);
            }

            try
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var type = assembly.GetType("UnityEditor.Connect.UnityConnect");
                    if (type == null)
                        continue;
                    object instance = GetSingleton(type);
                    if (instance == null)
                        continue;
                    foreach (var name in new[] { "GetAccessToken", "accessToken", "GetUserInfo" })
                    {
                        var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (method != null && method.GetParameters().Length == 0 && method.ReturnType == typeof(string))
                        {
                            string token = method.Invoke(instance, null) as string;
                            if (!string.IsNullOrEmpty(token))
                                return token;
                        }

                        var p = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (p != null && p.PropertyType == typeof(string))
                        {
                            string token = p.GetValue(instance) as string;
                            if (!string.IsNullOrEmpty(token))
                                return token;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("UnityConnect token: " + ex.Message);
            }

            return null;
        }

        static bool IsPresent(AssetSpec spec)
        {
            if (!Directory.Exists("Assets"))
                return false;

            foreach (var hint in spec.folderHints)
            {
                foreach (var dir in Directory.EnumerateDirectories("Assets", "*", SearchOption.AllDirectories))
                {
                    if (dir.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }

                foreach (var file in Directory.EnumerateFiles("Assets", "*", SearchOption.AllDirectories))
                {
                    if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (file.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }

            return false;
        }

        static string FindDownloadedPackage(AssetSpec spec)
        {
            string cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Unity", "Asset Store-5.x");
            if (!Directory.Exists(cache))
                return null;
            return Directory.EnumerateFiles(cache, "*.unitypackage", SearchOption.AllDirectories)
                .FirstOrDefault(f =>
                {
                    string name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
                    return name.Contains(spec.id) || spec.nameHints.Any(h => name.Contains(h));
                });
        }

        static void ConfigureIos()
        {
            Log("Configuring portrait iOS player settings...");
            PlayerSettings.companyName = "Solo Dreams";
            PlayerSettings.productName = "MyClicker";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.solodreams.myclicker");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.solodreams.myclicker");
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.hideHomeButton = true;
            PlayerSettings.statusBarHidden = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(NamedBuildTarget.iOS, 1);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.Low);
            PlayerSettings.SetMobileMTRendering(NamedBuildTarget.iOS, true);
            QualitySettings.vSyncCount = 0;

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
            {
                Log("Switching active build target to iOS...");
                EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.iOS, BuildTarget.iOS);
            }
        }

        static void CreateGameContent()
        {
            Log("Creating game scenes and config...");
            EnsureFolders();

            var config = FindOrCreateConfig();
            AssignSkin(config);
            AssignCatalogs(config);
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();

            CreateScene("Assets/MyClicker/Scenes/Boot.unity", SetupBootScene);
            CreateScene("Assets/MyClicker/Scenes/CharacterCreate.unity", SetupCharacterCreateScene);
            CreateScene("Assets/MyClicker/Scenes/Battle.unity", SetupBattleScene);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/MyClicker/Scenes/Boot.unity", true),
                new EditorBuildSettingsScene("Assets/MyClicker/Scenes/CharacterCreate.unity", true),
                new EditorBuildSettingsScene("Assets/MyClicker/Scenes/Battle.unity", true),
            };

            EditorSceneManager.OpenScene("Assets/MyClicker/Scenes/Boot.unity");
        }

        static MyClicker.Data.GameConfig FindOrCreateConfig()
        {
            const string path = "Assets/MyClicker/Resources/GameConfig.asset";
            if (!AssetDatabase.IsValidFolder("Assets/MyClicker/Resources"))
                AssetDatabase.CreateFolder("Assets/MyClicker", "Resources");
            var existing = AssetDatabase.LoadAssetAtPath<MyClicker.Data.GameConfig>(path);
            if (existing != null)
                return existing;
            var config = ScriptableObject.CreateInstance<MyClicker.Data.GameConfig>();
            AssetDatabase.CreateAsset(config, path);
            return config;
        }

        static void AssignSkin(MyClicker.Data.GameConfig config)
        {
            config.ui.panel = FindSprite("Window", "Panel", "Frame_Brown", "Board");
            config.ui.buttonNormal = FindSprite("Button01_Brown", "Button01_Blue", "Button_Square01");
            config.ui.buttonPressed = FindSprite("Button01_Red", "Button_Square03_p");
            config.ui.buttonDisabled = FindSprite("Button01_Gray", "Button_Circle01_Gray");
            config.ui.hpFill = FindSprite("Bar_Red", "Gauge_Red", "HP");
            config.ui.hpBackground = FindSprite("Bar_BG", "Gauge_BG", "Bar_Black");
            config.ui.coinIcon = FindSprite("Icon_Gold", "Coin", "Gold");
            config.ui.bannerReady = FindSprite("ActionText_Ready");
            config.ui.bannerVictory = FindSprite("ActionText_Victory", "ActionText_Win");
            config.ui.bannerLevelUp = FindSprite("ActionText_LevelUp1", "ActionText_LevelUp2");
        }

        static void AssignCatalogs(MyClicker.Data.GameConfig config)
        {
            config.character.heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/HeroEditor/FantasyHeroes/Prefabs/Human.prefab");
            config.character.partRoots = FindFolders("HeroEditor", "Character Maker", "CharacterMaker");
            config.combat.enemySprites = FindSpritesInFolders(new[] { "Miniature Army 2D V.1/Sprite", "Miniature Army" }, 48);
            config.world.backgroundSprites = FindSpritesInFolders(new[] { "2D Casual backgorund/Sprite", "Casual backgorund", "Casual Background" }, 32);
            config.economy.potionIcons = FindSpritesInFolders(new[] { "2D Potion Icon Pack/Sprites", "2D Potion Icon Pack" }, 64);

            var slots = new List<MyClicker.Data.GameConfig.SlotSprites>();
            foreach (var slot in config.character.slotOrder)
            {
                slots.Add(new MyClicker.Data.GameConfig.SlotSprites
                {
                    slot = slot,
                    sprites = FindSpritesInFolders(new[] { "Character Maker/" + slot, "CharacterMaker/" + slot, "/" + slot + "/" }, 80)
                });
            }

            config.character.slots = slots.ToArray();
        }

        static Sprite FindSprite(params string[] names)
        {
            foreach (var name in names)
            {
                foreach (var guid in AssetDatabase.FindAssets(name + " t:Sprite"))
                {
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
                    if (sprite != null)
                        return sprite;
                }

                foreach (var guid in AssetDatabase.FindAssets(name + " t:Texture2D"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
                    if (sprites.Length > 0)
                        return sprites[0];
                }
            }

            return null;
        }

        static Sprite[] FindSpritesInFolders(string[] folderHints, int max)
        {
            var list = new List<Sprite>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!folderHints.Any(h => path.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0))
                    continue;
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>();
                list.AddRange(sprites);
                if (list.Count >= max)
                    break;
            }

            return list.Take(max).ToArray();
        }

        static string[] FindFolders(params string[] hints)
        {
            var hits = new List<string>();
            foreach (var dir in Directory.EnumerateDirectories("Assets", "*", SearchOption.AllDirectories))
            {
                if (hints.Any(h => dir.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0))
                    hits.Add(dir.Replace('\\', '/'));
            }

            return hits.ToArray();
        }

        static void CreateScene(string path, Action<Scene> setup)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            setup(scene);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Assets");
            EditorSceneManager.SaveScene(scene, path);
        }

        static void SetupBootScene(Scene scene)
        {
            var cam = NewCamera("Main Camera", new Color(0.09f, 0.08f, 0.07f));
            var boot = new GameObject("Boot");
            boot.AddComponent<MyClicker.App.BootLoader>();
            AddEventSystem();
            AddCanvas("BootCanvas", includeSafeArea: true);
        }

        static void SetupCharacterCreateScene(Scene scene)
        {
            NewCamera("Main Camera", new Color(0.12f, 0.10f, 0.08f));
            AddEventSystem();
            var canvas = AddCanvas("CharacterCreateCanvas", includeSafeArea: true);
            var root = new GameObject("CharacterCreate");
            root.AddComponent<MyClicker.Character.CharacterCreatorController>();
        }

        static void SetupBattleScene(Scene scene)
        {
            NewCamera("Main Camera", new Color(0.18f, 0.22f, 0.16f));
            AddEventSystem();
            var world = new GameObject("World");
            world.AddComponent<MyClicker.World.BackgroundController>();
            var combat = new GameObject("Combat");
            combat.AddComponent<MyClicker.Combat.TapCombatController>();
            combat.AddComponent<MyClicker.Combat.EnemySpawner>();
            var canvas = AddCanvas("BattleCanvas", includeSafeArea: true);
            canvas.gameObject.AddComponent<MyClicker.UI.HudController>();
        }

        static Camera NewCamera(string name, Color clear)
        {
            var go = new GameObject(name);
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = clear;
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.nearClipPlane = -10f;
            cam.farClipPlane = 100f;
            go.AddComponent<AudioListener>();
            return cam;
        }

        static Canvas AddCanvas(string name, bool includeSafeArea)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            if (includeSafeArea)
            {
                var safe = new GameObject("SafeArea", typeof(RectTransform));
                safe.transform.SetParent(go.transform, false);
                var rt = safe.GetComponent<RectTransform>();
                Stretch(rt);
                safe.AddComponent<MyClicker.UI.SafeAreaFitter>();
            }

            return canvas;
        }

        static void AddEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void EnsureFolders()
        {
            string[] folders =
            {
                "Assets/MyClicker",
                "Assets/MyClicker/Editor",
                "Assets/MyClicker/Scripts",
                "Assets/MyClicker/Scripts/App",
                "Assets/MyClicker/Scripts/Character",
                "Assets/MyClicker/Scripts/Combat",
                "Assets/MyClicker/Scripts/Data",
                "Assets/MyClicker/Scripts/Economy",
                "Assets/MyClicker/Scripts/UI",
                "Assets/MyClicker/Scripts/World",
                "Assets/MyClicker/Scenes",
                "Assets/MyClicker/Data",
                "Assets/MyClicker/Resources",
                "Assets/MyClicker/Prefabs",
            };
            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
                    string child = Path.GetFileName(folder);
                    if (!string.IsNullOrEmpty(parent))
                        AssetDatabase.CreateFolder(parent, child);
                }
            }
        }

        static ImportState LoadState()
        {
            if (!File.Exists(StatePath))
                return new ImportState();
            try
            {
                return JsonUtility.FromJson<ImportState>(File.ReadAllText(StatePath)) ?? new ImportState();
            }
            catch
            {
                return new ImportState();
            }
        }

        static void SaveState(ImportState state)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StatePath) ?? ".");
            File.WriteAllText(StatePath, JsonUtility.ToJson(state, true));
        }

        static void Log(string message)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath) ?? ".");
            string line = DateTime.Now.ToString("HH:mm:ss") + "  " + message;
            File.AppendAllText(LogPath, line + Environment.NewLine);
            Debug.Log("[MyClicker] " + message);
        }

        static string ExtractJsonString(string json, string key)
        {
            string needle = "\"" + key + "\"";
            int i = json.IndexOf(needle, StringComparison.Ordinal);
            if (i < 0)
                return null;
            int colon = json.IndexOf(':', i + needle.Length);
            if (colon < 0)
                return null;
            int q1 = json.IndexOf('"', colon + 1);
            if (q1 < 0)
                return null;
            int q2 = json.IndexOf('"', q1 + 1);
            if (q2 < 0)
                return null;
            return json.Substring(q1 + 1, q2 - q1 - 1).Replace("\\/", "/");
        }

        static string Sanitize(string value)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value;
        }

        class AssetSpec
        {
            public readonly string id;
            public readonly string label;
            public readonly string[] nameHints;
            public readonly string[] folderHints;

            public AssetSpec(string id, string label, string[] nameHints, string[] folderHints)
            {
                this.id = id;
                this.label = label;
                this.nameHints = nameHints;
                this.folderHints = folderHints;
            }
        }

        [Serializable]
        class ImportState
        {
            public bool stoneQueued;
            public bool iosConfigured;
            public bool scenesCreated;
            public List<string> downloadStarted = new List<string>();
            public List<string> downloaded = new List<string>();
            public List<string> imported = new List<string>();
        }
    }
}
