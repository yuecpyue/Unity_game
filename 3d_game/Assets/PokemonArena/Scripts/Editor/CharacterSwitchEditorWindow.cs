#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PokemonArena
{
    public class CharacterSwitchEditorWindow : EditorWindow
    {
        private const string SevenPrefabPath = "Assets/model/seven_bone_Player.prefab";
        private const string ForgPrefabPath = "Assets/model/forg_bone_Player.prefab";

        [InitializeOnLoadMethod]
        private static void OnEditorStartup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ForgPrefabPath))
                {
                    ForgBoneCharacterSetup.CreateOrUpdateForgBonePrefabOnly();
                }
                CharacterSwitchManager switchMgr = Object.FindFirstObjectByType<CharacterSwitchManager>();
                if (switchMgr == null)
                {
                    SetupDualCharacterInActiveScene(saveScene: false);
                }
            };
        }

        [MenuItem("Tools/Character/🔄 Character Switcher Window", false, 0)]
        public static void OpenWindow()
        {
            CharacterSwitchEditorWindow window = GetWindow<CharacterSwitchEditorWindow>("Character Switcher");
            window.minSize = new Vector2(360, 480);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // Title
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("🎮 雙主角切換控制器", titleStyle);
            EditorGUILayout.LabelField("seven_bone (戰士) ⟷ forg_bone (青蛙)", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.Space(8);

            // Status Box
            Scene activeScene = EditorSceneManager.GetActiveScene();
            CharacterSwitchManager switchMgr = Object.FindFirstObjectByType<CharacterSwitchManager>();
            PlayerMovement[] players = Object.FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            GameObject sevenGo = null;
            GameObject forgGo = null;

            foreach (var p in players)
            {
                if (p == null) continue;
                string lower = p.gameObject.name.ToLower();
                if (lower.Contains("seven")) sevenGo = p.gameObject;
                else if (lower.Contains("forg") || lower.Contains("frog")) forgGo = p.gameObject;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"當前場景: {activeScene.name}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"切換管理器 (SwitchManager): {(switchMgr != null ? "✔ 已就緒" : "✖ 未加入")}");
            EditorGUILayout.LabelField($"主角 1 (seven_bone): {(sevenGo != null ? (sevenGo.activeSelf ? "✔ 啟用中 (Active)" : "○ 已載入 (Inactive)") : "✖ 場景中無")}");
            EditorGUILayout.LabelField($"主角 2 (forg_bone): {(forgGo != null ? (forgGo.activeSelf ? "✔ 啟用中 (Active)" : "○ 已載入 (Inactive)") : "✖ 場景中無")}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(12);

            // Switch Buttons (Play Mode or Edit Mode)
            EditorGUILayout.LabelField("【即時切換主角】", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = (sevenGo != null && sevenGo.activeSelf) ? new Color(0.3f, 0.9f, 0.4f) : Color.white;
            if (GUILayout.Button("👤 切換為 seven_bone\n(戰士)", GUILayout.Height(46)))
            {
                SwitchToCharacter(CharacterSwitchManager.CharacterType.SevenBone);
            }

            GUI.backgroundColor = (forgGo != null && forgGo.activeSelf) ? new Color(0.3f, 0.9f, 0.4f) : Color.white;
            if (GUILayout.Button("🐸 切換為 forg_bone\n(青蛙)", GUILayout.Height(46)))
            {
                SwitchToCharacter(CharacterSwitchManager.CharacterType.ForgBone);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(16);

            // Quick Setup Actions
            EditorGUILayout.LabelField("【場景配置工具】", EditorStyles.boldLabel);

            GUI.backgroundColor = new Color(0.25f, 0.7f, 1f);
            if (GUILayout.Button("⚡ 在當前場景建立雙主角切換系統", GUILayout.Height(36)))
            {
                SetupDualCharacterInActiveScene(saveScene: true);
            }

            GUI.backgroundColor = new Color(0.95f, 0.65f, 0.2f);
            if (GUILayout.Button("🚀 一鍵配置所有場景 (SampleScene & pokemon)", GUILayout.Height(36)))
            {
                SetupDualCharacterInAllScenes();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(16);

            // Prefab Rebuild Actions
            EditorGUILayout.LabelField("【角色 Prefab 產生器】", EditorStyles.boldLabel);

            if (GUILayout.Button("📦 生成 / 更新 forg_bone_Player.prefab"))
            {
                ForgBoneCharacterSetup.CreateOrUpdateForgBonePrefabOnly();
            }

            if (GUILayout.Button("📦 生成 / 更新 seven_bone_Player.prefab"))
            {
                SevenBoneCharacterSetup.SetupSevenBonePlayerInCurrentScene(saveScene: false);
            }

            EditorGUILayout.Space(12);

            // Operational instructions
            EditorGUILayout.HelpBox(
                "💡 執行提示：\n" +
                "• 進入 Play Mode 時，畫面上會出現半透明切換面板。\n" +
                "• 鍵盤按 [Tab] 可在兩個主角間即時輪替。\n" +
                "• 按鍵盤 [1] 或 [2] 可直接選定指定角色。\n" +
                "• 按 [C] 可隱藏/顯示操作介面。\n" +
                "• 雙角色均支援 WASD移動、Shift衝刺、Space跳躍、F/V揮手動作。",
                MessageType.Info);
        }

        public static void SwitchToCharacter(CharacterSwitchManager.CharacterType targetType)
        {
            if (Application.isPlaying)
            {
                CharacterSwitchManager switchMgr = Object.FindFirstObjectByType<CharacterSwitchManager>();
                if (switchMgr != null)
                {
                    switchMgr.SwitchTo(targetType);
                    return;
                }
            }

            // Edit Mode Switching
            SetupDualCharacterInActiveScene(saveScene: false, initialType: targetType);
            Scene activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            if (!Application.isPlaying)
            {
                EditorSceneManager.SaveScene(activeScene);
            }
        }

        public static void SetupDualCharacterInAllScenes()
        {
            // Ensure prefabs exist
            EnsureAllPrefabs();

            Scene activeScene = EditorSceneManager.GetActiveScene();
            string originalPath = activeScene.path;
            string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity", "Assets/Scenes/pokemon.unity" };

            foreach (var scPath in scenes)
            {
                if (File.Exists(scPath))
                {
                    EditorSceneManager.OpenScene(scPath, OpenSceneMode.Single);
                    SetupDualCharacterInActiveScene(saveScene: true, initialType: CharacterSwitchManager.CharacterType.SevenBone);
                }
            }

            if (!string.IsNullOrEmpty(originalPath) && File.Exists(originalPath))
            {
                EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
            }

            Debug.Log("<color=#4CAF50><b>[Character Switch]</b> All scenes configured with Dual Character Switch System!</color>");
        }

        public static void SetupDualCharacterInActiveScene(bool saveScene = true, CharacterSwitchManager.CharacterType initialType = CharacterSwitchManager.CharacterType.SevenBone)
        {
            EnsureAllPrefabs();

            // 1. Find or create SwitchManager
            CharacterSwitchManager switchMgr = Object.FindFirstObjectByType<CharacterSwitchManager>();
            if (switchMgr == null)
            {
                GameObject mgrGo = new GameObject("CharacterSwitchManager");
                switchMgr = mgrGo.AddComponent<CharacterSwitchManager>();
                Undo.RegisterCreatedObjectUndo(mgrGo, "Create CharacterSwitchManager");
            }

            // 2. Load Prefabs
            GameObject sevenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SevenPrefabPath);
            GameObject forgPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ForgPrefabPath);

            // 3. Find existing characters
            GameObject existingSeven = null;
            GameObject existingForg = null;
            PlayerMovement[] existingPlayers = Object.FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var p in existingPlayers)
            {
                if (p == null) continue;
                string lower = p.gameObject.name.ToLower();
                if (lower.Contains("seven")) existingSeven = p.gameObject;
                else if (lower.Contains("forg") || lower.Contains("frog")) existingForg = p.gameObject;
            }

            Vector3 spawnPos = new Vector3(0, 0.15f, -7.5f);
            Quaternion spawnRot = Quaternion.identity;

            if (existingSeven != null)
            {
                spawnPos = existingSeven.transform.position;
                spawnRot = existingSeven.transform.rotation;
            }
            else if (existingForg != null)
            {
                spawnPos = existingForg.transform.position;
                spawnRot = existingForg.transform.rotation;
            }

            // 4. Instantiate seven if missing
            if (existingSeven == null && sevenPrefab != null)
            {
                existingSeven = PrefabUtility.InstantiatePrefab(sevenPrefab) as GameObject;
                if (existingSeven == null) existingSeven = Object.Instantiate(sevenPrefab);
                existingSeven.name = "seven_bone";
                existingSeven.transform.position = spawnPos;
                existingSeven.transform.rotation = spawnRot;
                Undo.RegisterCreatedObjectUndo(existingSeven, "Instantiate seven_bone");
            }

            // 5. Instantiate forg if missing
            if (existingForg == null && forgPrefab != null)
            {
                existingForg = PrefabUtility.InstantiatePrefab(forgPrefab) as GameObject;
                if (existingForg == null) existingForg = Object.Instantiate(forgPrefab);
                existingForg.name = "forg_bone";
                existingForg.transform.position = spawnPos;
                existingForg.transform.rotation = spawnRot;
                Undo.RegisterCreatedObjectUndo(existingForg, "Instantiate forg_bone");
            }

            // 6. Set references in switchMgr
            switchMgr.sevenBoneObject = existingSeven;
            switchMgr.forgBoneObject = existingForg;
            switchMgr.sevenBonePrefab = sevenPrefab;
            switchMgr.forgBonePrefab = forgPrefab;
            switchMgr.currentType = initialType;

            // 7. Configure camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                ArenaCameraController camCtrl = mainCam.GetComponent<ArenaCameraController>();
                if (camCtrl == null) camCtrl = mainCam.gameObject.AddComponent<ArenaCameraController>();
                switchMgr.cameraController = camCtrl;

                GameObject initialGo = initialType == CharacterSwitchManager.CharacterType.SevenBone ? existingSeven : existingForg;
                if (initialGo != null)
                {
                    camCtrl.playerTarget = initialGo.transform;
                    camCtrl.SetMode(ArenaCameraController.CameraMode.FollowPlayer, immediate: true);
                }
            }

            // 8. Set active states according to initialType
            if (existingSeven != null) existingSeven.SetActive(initialType == CharacterSwitchManager.CharacterType.SevenBone);
            if (existingForg != null) existingForg.SetActive(initialType == CharacterSwitchManager.CharacterType.ForgBone);

            EditorUtility.SetDirty(switchMgr.gameObject);

            if (saveScene)
            {
                Scene activeScene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
            }

            Debug.Log($"<color=#4CAF50><b>[Character Switch]</b> Dual Character setup completed in {EditorSceneManager.GetActiveScene().name}!</color>");
        }

        private static void EnsureAllPrefabs()
        {
            if (!File.Exists(ForgPrefabPath))
            {
                ForgBoneCharacterSetup.CreateOrUpdateForgBonePrefabOnly();
            }
            if (!File.Exists(SevenPrefabPath))
            {
                SevenBoneCharacterSetup.SetupSevenBonePlayerInCurrentScene(saveScene: false);
            }
        }
    }
}
#endif
