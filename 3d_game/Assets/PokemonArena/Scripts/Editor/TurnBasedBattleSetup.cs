#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

namespace PokemonArena
{
    /// <summary>
    /// 回合制戰鬥一鍵建置與輔助工具 (涵蓋階段一、二、三)
    /// </summary>
    public static class TurnBasedBattleSetup
    {
        private const string ControllerPath = "Assets/Animations/hero_Animator.controller";
        private const string ShurikenPrefabPath = "Assets/PokemonArena/Prefabs/WaterShuriken.prefab";
        private const string ShurikenMaterialPath = "Assets/PokemonArena/Materials/Mat_WaterShuriken.mat";

        [InitializeOnLoadMethod]
        private static void OnEditorReload()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ShurikenPrefabPath))
                {
                    CreateWaterShurikenPrefab();
                }
                SetupAnimatorController();
            };
        }

        [MenuItem("Tools/Turn-Based Battle/1. 設定 Animator (加入 Attack 與 Hit 參數及狀態)", false, 1)]
        public static void SetupAnimatorMenu()
        {
            SetupAnimatorController();
        }

        [MenuItem("Tools/Turn-Based Battle/2. 生成水手裡劍 Prefab (Water Shuriken)", false, 2)]
        public static void CreateShurikenPrefabMenu()
        {
            CreateWaterShurikenPrefab();
        }

        [MenuItem("Tools/Turn-Based Battle/3. 一鍵搭建完整回合制戰鬥場景 (Pokemon Arena)", false, 3)]
        public static void SetupCompleteBattleSceneMenu()
        {
            SetupCompleteBattleScene();
        }

        [MenuItem("Tools/Turn-Based Battle/4. 搭建階段一測試 Cube 敵人場景", false, 4)]
        public static void SetupPhase1TestSceneMenu()
        {
            SetupPhase1CubeTestScene();
        }

        [MenuItem("Tools/Turn-Based Battle/5. 一鍵搭建即時天氣連動與技能測試 UI (Digital Twin)", false, 5)]
        public static void SetupWeatherSystemMenu()
        {
            SetupWeatherSystem();
        }

        /// <summary>
        /// [階段二] 為 hero_Animator.controller 加入 Attack 與 Hit 觸發器與狀態轉換
        /// </summary>
        public static void SetupAnimatorController()
        {
            AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (ctrl == null)
            {
                ctrl = SevenBoneCharacterSetup.EnsureHeroAnimatorController();
            }

            // 確保 Trigger 參數存在
            EnsureParameter(ctrl, "Attack", AnimatorControllerParameterType.Trigger);
            EnsureParameter(ctrl, "Hit", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

            AnimationClip idleClip = LoadFirstClip("Assets/Animations/Breathing Idle.fbx");
            AnimationClip waveClip = LoadFirstClip("Assets/Animations/Waving.fbx");
            AnimationClip jumpClip = LoadFirstClip("Assets/Animations/Jumping.fbx");

            AnimatorState idleState = GetOrCreateState(sm, "Idle", idleClip);
            AnimatorState attackState = GetOrCreateState(sm, "Attack", waveClip);
            AnimatorState hitState = GetOrCreateState(sm, "Hit", jumpClip);

            // AnyState -> Attack
            EnsureAnyStateTransition(sm, attackState, "Attack");
            // Attack -> Idle
            EnsureExitTransition(attackState, idleState, 0.75f);

            // AnyState -> Hit
            EnsureAnyStateTransition(sm, hitState, "Hit");
            // Hit -> Idle
            EnsureExitTransition(hitState, idleState, 0.6f);

            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=#4CAF50><b>[Battle Setup]</b> hero_Animator 已成功加入 Attack 與 Hit 觸發器與狀態轉換！</color>");
        }

        /// <summary>
        /// [階段三] 生成水手裡劍 3D 模型 Prefab 與專屬水流發光材質
        /// </summary>
        public static GameObject CreateWaterShurikenPrefab()
        {
            // 1. 確保材質資料夾存在
            if (!Directory.Exists("Assets/PokemonArena/Materials"))
            {
                Directory.CreateDirectory("Assets/PokemonArena/Materials");
            }
            if (!Directory.Exists("Assets/PokemonArena/Prefabs"))
            {
                Directory.CreateDirectory("Assets/PokemonArena/Prefabs");
            }

            // 2. 建立水手裡劍專用發光材質 (URP Lit)
            Material shurikenMat = AssetDatabase.LoadAssetAtPath<Material>(ShurikenMaterialPath);
            if (shurikenMat == null)
            {
                Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
                if (urpLit == null) urpLit = Shader.Find("Standard");
                shurikenMat = new Material(urpLit);
                shurikenMat.name = "Mat_WaterShuriken";

                Color waterCyan = new Color(0.0f, 0.85f, 1.0f, 0.9f);
                if (shurikenMat.HasProperty("_BaseColor"))
                {
                    shurikenMat.SetColor("_BaseColor", waterCyan);
                }
                else if (shurikenMat.HasProperty("_Color"))
                {
                    shurikenMat.SetColor("_Color", waterCyan);
                }

                // 開啟發光
                if (shurikenMat.HasProperty("_EmissionColor"))
                {
                    shurikenMat.EnableKeyword("_EMISSION");
                    shurikenMat.SetColor("_EmissionColor", new Color(0.0f, 0.9f, 1.0f) * 2.0f);
                }

                AssetDatabase.CreateAsset(shurikenMat, ShurikenMaterialPath);
            }

            // 3. 建立水手裡劍立體星芒 Mesh
            Mesh shurikenMesh = CreateShurikenMesh();

            // 4. 建立 Prefab 遊戲物件
            GameObject shurikenGo = new GameObject("WaterShuriken");
            MeshFilter mf = shurikenGo.AddComponent<MeshFilter>();
            mf.sharedMesh = shurikenMesh;
            MeshRenderer mr = shurikenGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = shurikenMat;

            // 碰撞與觸發器
            SphereCollider col = shurikenGo.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.5f;

            // 掛載 Projectile 腳本
            shurikenGo.AddComponent<Projectile>();

            // 儲存為 Prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(shurikenGo, ShurikenPrefabPath);
            Object.DestroyImmediate(shurikenGo);

            AssetDatabase.SaveAssets();
            Debug.Log($"<color=#00E5FF><b>[Battle Setup]</b> 水手裡劍 Prefab 已成功生成於: {ShurikenPrefabPath}</color>");
            return prefab;
        }

        /// <summary>
        /// 程式化生成四角水手裡劍立體網格
        /// </summary>
        private static Mesh CreateShurikenMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "Mesh_WaterShuriken";

            float outerR = 0.5f;
            float innerR = 0.15f;
            float thickness = 0.05f;

            // 8 個外輪廓頂點
            Vector3[] ring = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI / 4f;
                float r = (i % 2 == 0) ? outerR : innerR;
                ring[i] = new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r, 0);
            }

            // 頂點：中心正反 2 點 + 輪廓 8 點
            Vector3 frontCenter = new Vector3(0, 0, thickness);
            Vector3 backCenter = new Vector3(0, 0, -thickness);

            Vector3[] vertices = new Vector3[18];
            vertices[0] = frontCenter;
            vertices[1] = backCenter;
            for (int i = 0; i < 8; i++)
            {
                vertices[2 + i] = ring[i];               // 前半面輪廓
                vertices[10 + i] = ring[i];              // 後半面輪廓
            }

            int[] triangles = new int[8 * 3 * 2];
            int triIndex = 0;

            // 前側面 8 個三角形
            for (int i = 0; i < 8; i++)
            {
                int next = (i + 1) % 8;
                triangles[triIndex++] = 0;
                triangles[triIndex++] = 2 + next;
                triangles[triIndex++] = 2 + i;
            }

            // 後側面 8 個三角形
            for (int i = 0; i < 8; i++)
            {
                int next = (i + 1) % 8;
                triangles[triIndex++] = 1;
                triangles[triIndex++] = 10 + i;
                triangles[triIndex++] = 10 + next;
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// 一鍵搭建完整回合制戰鬥場景 (涵蓋場地、甲賀忍蛙、敵方角色、BattleSystem、UI Canvas)
        /// </summary>
        public static void SetupCompleteBattleScene()
        {
            // 1. 確保 Animator 與 Prefab 就緒
            SetupAnimatorController();
            GameObject shurikenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShurikenPrefabPath);
            if (shurikenPrefab == null)
            {
                shurikenPrefab = CreateWaterShurikenPrefab();
            }

            // 2. 確保 EventSystem 存在 (UI 點擊必須)
            EnsureEventSystem();

            // 3. 確保玩家角色 (甲賀忍蛙)
            GameObject playerGo = GameObject.Find("Player_Greninja");
            if (playerGo == null)
            {
                GameObject forgPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ForgBoneCharacterSetup.PrefabPath);
                if (forgPrefab != null)
                {
                    playerGo = PrefabUtility.InstantiatePrefab(forgPrefab) as GameObject;
                }
                else
                {
                    GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ForgBoneCharacterSetup.ForgBoneFbxPath);
                    if (fbx != null) playerGo = Object.Instantiate(fbx);
                }
                if (playerGo != null)
                {
                    playerGo.name = "Player_Greninja";
                    playerGo.transform.position = new Vector3(0, 0.15f, -6.5f);
                    playerGo.transform.rotation = Quaternion.identity;
                }
            }

            // 設置 Player BattleUnit
            BattleUnit playerUnit = null;
            if (playerGo != null)
            {
                playerUnit = playerGo.GetComponent<BattleUnit>();
                if (playerUnit == null) playerUnit = playerGo.AddComponent<BattleUnit>();

                // 透過 SerializedObject 設置數值
                SerializedObject so = new SerializedObject(playerUnit);
                so.FindProperty("unitName").stringValue = "甲賀忍蛙";
                so.FindProperty("maxHP").intValue = 100;
                so.FindProperty("damage").intValue = 30;
                so.FindProperty("projectilePrefab").objectReferenceValue = shurikenPrefab;
                so.ApplyModifiedProperties();

                // 關閉 PlayerMovement 避免干擾回合戰鬥
                PlayerMovement pm = playerGo.GetComponent<PlayerMovement>();
                if (pm != null) pm.enabled = false;
            }

            // 4. 確保敵方角色
            GameObject enemyGo = GameObject.Find("Enemy_Target");
            if (enemyGo == null)
            {
                GameObject sevenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/model/seven_bone_Player.prefab");
                if (sevenPrefab != null)
                {
                    enemyGo = PrefabUtility.InstantiatePrefab(sevenPrefab) as GameObject;
                }
                else
                {
                    GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/model/seven_bone.fbx");
                    if (fbx != null) enemyGo = Object.Instantiate(fbx);
                }

                if (enemyGo != null)
                {
                    enemyGo.name = "Enemy_Target";
                    enemyGo.transform.position = new Vector3(0, 0.15f, 6.5f);
                    enemyGo.transform.rotation = Quaternion.Euler(0, 180, 0);
                }
            }

            BattleUnit enemyUnit = null;
            if (enemyGo != null)
            {
                enemyUnit = enemyGo.GetComponent<BattleUnit>();
                if (enemyUnit == null) enemyUnit = enemyGo.AddComponent<BattleUnit>();

                SerializedObject so = new SerializedObject(enemyUnit);
                so.FindProperty("unitName").stringValue = "暗黑戰士 (敵人)";
                so.FindProperty("maxHP").intValue = 90;
                so.FindProperty("damage").intValue = 20;
                so.ApplyModifiedProperties();

                PlayerMovement pm = enemyGo.GetComponent<PlayerMovement>();
                if (pm != null) pm.enabled = false;
            }

            // 5. 建立 BattleManager 與掛載 BattleSystem
            GameObject bmGo = GameObject.Find("BattleManager");
            if (bmGo == null)
            {
                bmGo = new GameObject("BattleManager");
            }
            BattleSystem battleSys = bmGo.GetComponent<BattleSystem>();
            if (battleSys == null) battleSys = bmGo.AddComponent<BattleSystem>();

            // 6. 建立 UI Canvas
            Canvas battleCanvas = BuildBattleUICanvas(battleSys, playerUnit, enemyUnit);

            // 7. 綁定 BattleSystem 欄位
            SerializedObject sysSo = new SerializedObject(battleSys);
            sysSo.FindProperty("playerUnit").objectReferenceValue = playerUnit;
            sysSo.FindProperty("enemyUnit").objectReferenceValue = enemyUnit;
            sysSo.FindProperty("waterShurikenPrefab").objectReferenceValue = shurikenPrefab;
            sysSo.ApplyModifiedProperties();

            // 調整攝影機觀戰視角
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(-8.5f, 4.5f, 0f);
                mainCam.transform.rotation = Quaternion.Euler(22f, 90f, 0f);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=#76FF03><b>[Battle Setup]</b> 回合制戰鬥場景已成功搭建完成！請點擊 Play 進行遊玩！</color>");
        }

        /// <summary>
        /// [階段一] 快速搭建包含 Cube 敵人的測試場景
        /// </summary>
        public static void SetupPhase1CubeTestScene()
        {
            EnsureEventSystem();

            // 1. 建立地面
            GameObject plane = GameObject.Find("Battle_Ground");
            if (plane == null)
            {
                plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                plane.name = "Battle_Ground";
                plane.transform.position = Vector3.zero;
                plane.transform.localScale = new Vector3(3, 1, 3);
            }

            // 2. 建立玩家 (Player_Greninja)
            GameObject playerGo = GameObject.Find("Player_Greninja");
            if (playerGo == null)
            {
                GameObject forgPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ForgBoneCharacterSetup.PrefabPath);
                if (forgPrefab != null)
                {
                    playerGo = PrefabUtility.InstantiatePrefab(forgPrefab) as GameObject;
                }
                else
                {
                    playerGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                }
                playerGo.name = "Player_Greninja";
            }
            playerGo.transform.position = new Vector3(0, 0, -4f);
            playerGo.transform.rotation = Quaternion.identity;

            BattleUnit playerUnit = playerGo.GetComponent<BattleUnit>();
            if (playerUnit == null) playerUnit = playerGo.AddComponent<BattleUnit>();
            SerializedObject pSo = new SerializedObject(playerUnit);
            pSo.FindProperty("unitName").stringValue = "甲賀忍蛙";
            pSo.FindProperty("maxHP").intValue = 100;
            pSo.FindProperty("damage").intValue = 30;
            pSo.ApplyModifiedProperties();

            // 3. 建立測試用 Cube 敵人
            GameObject cubeEnemy = GameObject.Find("TestEnemy_Cube");
            if (cubeEnemy == null)
            {
                cubeEnemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cubeEnemy.name = "TestEnemy_Cube";
            }
            cubeEnemy.transform.position = new Vector3(0, 0.5f, 4f);
            cubeEnemy.transform.rotation = Quaternion.Euler(0, 180, 0);

            // 賦予 Cube 一個紅色測試材質以利觀察閃白
            MeshRenderer mr = cubeEnemy.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material redMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                redMat.color = new Color(0.9f, 0.2f, 0.2f);
                mr.material = redMat;
            }

            BattleUnit enemyUnit = cubeEnemy.GetComponent<BattleUnit>();
            if (enemyUnit == null) enemyUnit = cubeEnemy.AddComponent<BattleUnit>();
            SerializedObject eSo = new SerializedObject(enemyUnit);
            eSo.FindProperty("unitName").stringValue = "測試木樁 (Cube)";
            eSo.FindProperty("maxHP").intValue = 60;
            eSo.FindProperty("damage").intValue = 15;
            eSo.ApplyModifiedProperties();

            // 4. 建立 BattleManager
            GameObject bmGo = GameObject.Find("BattleManager");
            if (bmGo == null) bmGo = new GameObject("BattleManager");
            BattleSystem battleSys = bmGo.GetComponent<BattleSystem>();
            if (battleSys == null) battleSys = bmGo.AddComponent<BattleSystem>();

            SerializedObject sysSo = new SerializedObject(battleSys);
            sysSo.FindProperty("playerUnit").objectReferenceValue = playerUnit;
            sysSo.FindProperty("enemyUnit").objectReferenceValue = enemyUnit;
            sysSo.FindProperty("autoPlayIfNoUI").boolValue = true; // 階段一自動出招以利 Console 觀察
            sysSo.ApplyModifiedProperties();

            // 攝影機
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(-6f, 3.5f, 0f);
                cam.transform.rotation = Quaternion.Euler(20f, 90f, 0f);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=#00E5FF><b>[Battle Setup]</b> 階段一測試場景 (Cube 敵人) 已建立完成！按 Play 即可在 Console 觀察回合狀態轉換！</color>");
        }

        /// <summary>
        /// [即時天氣連動] 一鍵建置 WeatherBattleManager、測試 UI 與按鈕事件
        /// </summary>
        public static void SetupWeatherSystem()
        {
            EnsureEventSystem();

            // 1. 確保 StreamingAssets/weather.json 存在
            string streamingPath = Path.Combine(Application.dataPath, "StreamingAssets");
            if (!Directory.Exists(streamingPath)) Directory.CreateDirectory(streamingPath);
            string jsonPath = Path.Combine(streamingPath, "weather.json");
            if (!File.Exists(jsonPath))
            {
                string sampleJson = "{\n  \"records\": {\n    \"Station\": [\n      {\n        \"StationName\": \"臺北\",\n        \"ObsTime\": { \"DateTime\": \"2026-11-17T14:00:00+08:00\" },\n        \"WeatherElement\": {\n          \"Weather\": \"雨\",\n          \"AirTemperature\": 20.5,\n          \"RelativeHumidity\": 85\n        }\n      }\n    ]\n  }\n}";
                File.WriteAllText(jsonPath, sampleJson);
                AssetDatabase.Refresh();
            }

            // 2. 建立或取得 Canvas
            GameObject canvasGo = GameObject.Find("BattleCanvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("BattleCanvas");
                Canvas canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            // 3. 建立 WeatherStatusText
            GameObject weatherTextGo = GameObject.Find("WeatherStatusText");
            if (weatherTextGo == null)
            {
                weatherTextGo = CreateTMPText(canvasGo.transform, "WeatherStatusText", "Weather: Loading...", new Vector2(0, 340), new Vector2(700, 110), 18, TextAlignmentOptions.Center);
            }
            TextMeshProUGUI weatherTMP = weatherTextGo.GetComponent<TextMeshProUGUI>();

            // 4. 建立 WeatherBattleLogText
            GameObject battleLogGo = GameObject.Find("WeatherBattleLogText");
            if (battleLogGo == null)
            {
                battleLogGo = CreateTMPText(canvasGo.transform, "WeatherBattleLogText", "Press Button to Cast Water Shuriken...", new Vector2(0, -120), new Vector2(650, 50), 18, TextAlignmentOptions.Center);
            }
            TextMeshProUGUI battleLogTMP = battleLogGo.GetComponent<TextMeshProUGUI>();

            // 5. 建立 WeatherTestButton
            GameObject testBtnGo = GameObject.Find("WeatherTestButton");
            if (testBtnGo == null)
            {
                testBtnGo = CreateButton(canvasGo.transform, "WeatherTestButton", "Cast Water Shuriken (Test)", new Vector2(0, -60), new Color(0f, 0.6f, 0.9f));
            }
            Button testBtn = testBtnGo.GetComponent<Button>();

            // 6. 建立 WeatherBattleManager
            GameObject wbmGo = GameObject.Find("WeatherBattleManager");
            if (wbmGo == null) wbmGo = new GameObject("WeatherBattleManager");
            WeatherBattleManager wbm = wbmGo.GetComponent<WeatherBattleManager>();
            if (wbm == null) wbm = wbmGo.AddComponent<WeatherBattleManager>();

            SerializedObject wbmSo = new SerializedObject(wbm);
            wbmSo.FindProperty("stationName").stringValue = "臺北";
            wbmSo.FindProperty("useLocalFile").boolValue = true; // 預設使用本機測試檔以利立即測試
            wbmSo.FindProperty("pollInterval").floatValue = 300f;
            wbmSo.FindProperty("weatherStatusText").objectReferenceValue = weatherTMP;
            wbmSo.ApplyModifiedProperties();

            // 7. 建立 BattleWeatherTest
            BattleWeatherTest bwt = wbmGo.GetComponent<BattleWeatherTest>();
            if (bwt == null) bwt = wbmGo.AddComponent<BattleWeatherTest>();

            SerializedObject bwtSo = new SerializedObject(bwt);
            bwtSo.FindProperty("baseWaterDamage").floatValue = 100f;
            bwtSo.FindProperty("weatherManager").objectReferenceValue = wbm;
            bwtSo.FindProperty("battleLogText").objectReferenceValue = battleLogTMP;
            bwtSo.ApplyModifiedProperties();

            // 綁定 Button 點擊事件
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(testBtn.onClick, bwt.CastWaterShuriken);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(testBtn.onClick, bwt.CastWaterShuriken);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=#00E5FF><b>[Weather Setup]</b> 即時氣象連動與技能測試 UI 已成功建置！可直接點擊 Play 或 Inspector 的 Refresh Now 測試！</color>");
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esGo.AddComponent<InputSystemUIInputModule>();
#else
                esGo.AddComponent<StandaloneInputModule>();
#endif
            }
        }

        /// <summary>
        /// 建立戰鬥 UI Canvas 與 Slider、Button、TMP 元件
        /// </summary>
        private static Canvas BuildBattleUICanvas(BattleSystem battleSys, BattleUnit player, BattleUnit enemy)
        {
            GameObject canvasGo = GameObject.Find("BattleCanvas");
            if (canvasGo != null) Object.DestroyImmediate(canvasGo);

            canvasGo = new GameObject("BattleCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            // 1. 頂部狀態文字
            GameObject statusTextGo = CreateTMPText(canvasGo.transform, "StatusText", "戰鬥準備開始...", new Vector2(0, -60), new Vector2(600, 60), 24, TextAlignmentOptions.Center);
            TextMeshProUGUI statusTMP = statusTextGo.GetComponent<TextMeshProUGUI>();

            // 2. 玩家血條 Slider
            GameObject playerSliderGo = CreateHPSlider(canvasGo.transform, "PlayerHPSlider", new Vector2(-280, -220), new Color(0.2f, 0.8f, 0.2f));
            Slider playerSlider = playerSliderGo.GetComponent<Slider>();
            GameObject playerLabelGo = CreateTMPText(canvasGo.transform, "PlayerHPLabel", "甲賀忍蛙\nHP: 100/100", new Vector2(-280, -170), new Vector2(240, 50), 18, TextAlignmentOptions.Center);
            TextMeshProUGUI playerLabelTMP = playerLabelGo.GetComponent<TextMeshProUGUI>();

            // 3. 敵人血條 Slider
            GameObject enemySliderGo = CreateHPSlider(canvasGo.transform, "EnemyHPSlider", new Vector2(280, -220), new Color(0.9f, 0.2f, 0.2f));
            Slider enemySlider = enemySliderGo.GetComponent<Slider>();
            GameObject enemyLabelGo = CreateTMPText(canvasGo.transform, "EnemyHPLabel", "敵人\nHP: 90/90", new Vector2(280, -170), new Vector2(240, 50), 18, TextAlignmentOptions.Center);
            TextMeshProUGUI enemyLabelTMP = enemyLabelGo.GetComponent<TextMeshProUGUI>();

            // 4. 近戰攻擊按鈕
            GameObject meleeBtnGo = CreateButton(canvasGo.transform, "MeleeAttackButton", "衝刺近戰 (Melee)", new Vector2(-120, -320), new Color(0.2f, 0.45f, 0.85f));
            Button meleeBtn = meleeBtnGo.GetComponent<Button>();

            // 5. 遠程水手裡劍按鈕
            GameObject rangedBtnGo = CreateButton(canvasGo.transform, "RangedAttackButton", "水手裡劍 (Shuriken)", new Vector2(120, -320), new Color(0.1f, 0.75f, 0.85f));
            Button rangedBtn = rangedBtnGo.GetComponent<Button>();

            // 綁定按鈕 OnClick 事件至 BattleSystem
            UnityEditor.Events.UnityEventTools.AddPersistentListener(meleeBtn.onClick, battleSys.OnMeleeAttackButton);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(rangedBtn.onClick, battleSys.OnRangedAttackButton);

            // 綁定到 BattleSystem 變數
            SerializedObject so = new SerializedObject(battleSys);
            so.FindProperty("meleeAttackButton").objectReferenceValue = meleeBtn;
            so.FindProperty("rangedAttackButton").objectReferenceValue = rangedBtn;
            so.FindProperty("playerHPSlider").objectReferenceValue = playerSlider;
            so.FindProperty("enemyHPSlider").objectReferenceValue = enemySlider;
            so.FindProperty("playerHPText").objectReferenceValue = playerLabelTMP;
            so.FindProperty("enemyHPText").objectReferenceValue = enemyLabelTMP;
            so.FindProperty("battleStatusText").objectReferenceValue = statusTMP;
            so.ApplyModifiedProperties();

            return canvas;
        }

        private static GameObject CreateHPSlider(Transform parent, string name, Vector2 anchoredPos, Color fillColor)
        {
            GameObject sliderGo = new GameObject(name);
            sliderGo.transform.SetParent(parent, false);
            RectTransform rt = sliderGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(220, 24);
            rt.anchoredPosition = anchoredPos;

            Slider slider = sliderGo.AddComponent<Slider>();

            // Background
            GameObject bgGo = new GameObject("Background");
            bgGo.transform.SetParent(sliderGo.transform, false);
            RectTransform bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            // Fill Area
            GameObject fillAreaGo = new GameObject("Fill Area");
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            RectTransform faRt = fillAreaGo.AddComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero;
            faRt.anchorMax = Vector2.one;
            faRt.sizeDelta = new Vector2(-10, 0);

            // Fill
            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            RectTransform fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.sizeDelta = Vector2.zero;
            Image fillImg = fillGo.AddComponent<Image>();
            fillImg.color = fillColor;

            slider.fillRect = fillRt;
            slider.targetGraphic = fillImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.value = 100;

            return sliderGo;
        }

        private static GameObject CreateButton(Transform parent, string name, string labelText, Vector2 anchoredPos, Color btnColor)
        {
            GameObject btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);
            RectTransform rt = btnGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(180, 48);
            rt.anchoredPosition = anchoredPos;

            Image img = btnGo.AddComponent<Image>();
            img.color = btnColor;

            Button btn = btnGo.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = btnColor * 1.2f;
            cb.pressedColor = btnColor * 0.8f;
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);
            btn.colors = cb;

            CreateTMPText(btnGo.transform, "Text", labelText, Vector2.zero, new Vector2(180, 48), 16, TextAlignmentOptions.Center);
            return btnGo;
        }

        private static GameObject CreateTMPText(Transform parent, string name, string text, Vector2 anchoredPos, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject textGo = new GameObject(name);
            textGo.transform.SetParent(parent, false);
            RectTransform rt = textGo.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return textGo;
        }

        private static void EnsureParameter(AnimatorController ctrl, string name, AnimatorControllerParameterType type)
        {
            foreach (var p in ctrl.parameters)
            {
                if (p.name == name) return;
            }
            ctrl.AddParameter(name, type);
        }

        private static AnimatorState GetOrCreateState(AnimatorStateMachine sm, string stateName, Motion motion)
        {
            foreach (var childState in sm.states)
            {
                if (childState.state.name == stateName)
                {
                    if (motion != null) childState.state.motion = motion;
                    return childState.state;
                }
            }
            AnimatorState st = sm.AddState(stateName);
            if (motion != null) st.motion = motion;
            return st;
        }

        private static void EnsureAnyStateTransition(AnimatorStateMachine sm, AnimatorState targetState, string triggerName)
        {
            foreach (var trans in sm.anyStateTransitions)
            {
                if (trans.destinationState == targetState) return;
            }
            var t = sm.AddAnyStateTransition(targetState);
            t.AddCondition(AnimatorConditionMode.If, 0, triggerName);
            t.duration = 0.1f;
            t.hasExitTime = false;
        }

        private static void EnsureExitTransition(AnimatorState fromState, AnimatorState toState, float exitTime)
        {
            foreach (var trans in fromState.transitions)
            {
                if (trans.destinationState == toState) return;
            }
            var t = fromState.AddTransition(toState);
            t.hasExitTime = true;
            t.exitTime = exitTime;
            t.duration = 0.2f;
        }

        private static AnimationClip LoadFirstClip(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (a is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }
    }
}
#endif
