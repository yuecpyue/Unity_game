using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PokemonArena
{
    public class CharacterSwitchManager : MonoBehaviour
    {
        public enum CharacterType
        {
            SevenBone = 0,
            ForgBone = 1
        }

        [Header("Character References")]
        [Tooltip("The seven_bone player instance in the scene")]
        public GameObject sevenBoneObject;

        [Tooltip("The forg_bone player instance in the scene")]
        public GameObject forgBoneObject;

        [Header("Prefabs (Fallback if not instantiated)")]
        public GameObject sevenBonePrefab;
        public GameObject forgBonePrefab;

        [Header("Active Character State")]
        public CharacterType currentType = CharacterType.SevenBone;

        [Header("Camera & UI")]
        public ArenaCameraController cameraController;
        public bool showOnScreenUI = true;
        public bool allowHotkeys = true;

        [Header("Keyboard Shortcuts")]
        public KeyCode toggleUIKey = KeyCode.C;
        public KeyCode switchNextKey = KeyCode.Tab;
        public KeyCode selectSevenKey = KeyCode.Alpha1;
        public KeyCode selectForgKey = KeyCode.Alpha2;

        public event Action<CharacterType> OnCharacterChanged;

        private Rect windowRect = new Rect(20, 20, 270, 220);
        private bool isMinimized = false;
        private GUIStyle windowStyle;
        private GUIStyle titleStyle;
        private GUIStyle buttonActiveStyle;
        private GUIStyle buttonNormalStyle;
        private GUIStyle statusStyle;
        private GUIStyle tipStyle;
        private bool stylesInitialized = false;

        private const string SevenPrefabPath = "Assets/model/seven_bone_Player.prefab";
        private const string ForgPrefabPath = "Assets/model/forg_bone_Player.prefab";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitInPlayMode()
        {
            if (FindFirstObjectByType<CharacterSwitchManager>() == null)
            {
                // Only create if there is a player or arena in the scene
                PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
                ArenaCameraController cam = FindFirstObjectByType<ArenaCameraController>();
                if (pm != null || cam != null)
                {
                    GameObject go = new GameObject("CharacterSwitchManager");
                    go.AddComponent<CharacterSwitchManager>();
                }
            }
        }

        private void Awake()
        {
            if (cameraController == null && Camera.main != null)
            {
                cameraController = Camera.main.GetComponent<ArenaCameraController>();
            }

            FindOrCreateCharacters();
        }

        private void Start()
        {
            // Activate the initial character
            ApplyActiveCharacter(currentType, transferTransform: false);
        }

        private void Update()
        {
            if (!allowHotkeys) return;

            // 1. Toggle UI
            if (ReadKeyDown(toggleUIKey, Key.C))
            {
                showOnScreenUI = !showOnScreenUI;
            }

            // 2. Next Character (Tab)
            if (ReadKeyDown(switchNextKey, Key.Tab))
            {
                SwitchTo(currentType == CharacterType.SevenBone ? CharacterType.ForgBone : CharacterType.SevenBone);
            }

            // 3. Direct selection (1 / 2)
            if (ReadKeyDown(selectSevenKey, Key.Digit1) || ReadKeyDirect(Key.Numpad1))
            {
                SwitchTo(CharacterType.SevenBone);
            }
            else if (ReadKeyDown(selectForgKey, Key.Digit2) || ReadKeyDirect(Key.Numpad2))
            {
                SwitchTo(CharacterType.ForgBone);
            }
        }

        public void SwitchTo(CharacterType newType)
        {
            if (currentType == newType && GetCharacterObject(newType) != null && GetCharacterObject(newType).activeSelf)
            {
                return;
            }

            ApplyActiveCharacter(newType, transferTransform: true);
            currentType = newType;
            OnCharacterChanged?.Invoke(newType);
        }

        private void ApplyActiveCharacter(CharacterType type, bool transferTransform)
        {
            GameObject prevGo = GetCurrentActiveObject();
            Vector3 prevPos = Vector3.zero;
            Quaternion prevRot = Quaternion.identity;

            if (prevGo != null)
            {
                prevPos = prevGo.transform.position;
                prevRot = prevGo.transform.rotation;
            }
            else
            {
                prevPos = new Vector3(0, 0.15f, -7.5f);
                prevRot = Quaternion.identity;
            }

            // Ensure instances exist
            EnsureCharacterInstance(type);

            GameObject nextGo = GetCharacterObject(type);
            if (nextGo == null)
            {
                Debug.LogError($"[CharacterSwitchManager] Cannot switch to {type}: object is null.");
                return;
            }

            // Transfer position & rotation seamlessly
            if (transferTransform && prevGo != null)
            {
                CharacterController nextCc = nextGo.GetComponent<CharacterController>();
                if (nextCc != null) nextCc.enabled = false;
                nextGo.transform.position = prevPos;
                nextGo.transform.rotation = prevRot;
                if (nextCc != null) nextCc.enabled = true;
            }

            // Toggle active states
            if (sevenBoneObject != null) sevenBoneObject.SetActive(type == CharacterType.SevenBone);
            if (forgBoneObject != null) forgBoneObject.SetActive(type == CharacterType.ForgBone);

            // Update camera target and force follow mode so the camera never freezes
            if (cameraController == null && Camera.main != null)
            {
                cameraController = Camera.main.GetComponent<ArenaCameraController>();
            }

            if (cameraController != null)
            {
                cameraController.SetTarget(nextGo.transform, forceFollow: true);
            }

            // Update PlayerMovement camera ref
            PlayerMovement pm = nextGo.GetComponent<PlayerMovement>();
            if (pm != null && Camera.main != null)
            {
                pm.cameraTransform = Camera.main.transform;
            }

            Debug.Log($"<color=#4CAF50><b>[Character Switch]</b> Switched protagonist to: <b>{nextGo.name}</b></color>");
        }

        public GameObject GetCurrentActiveObject()
        {
            if (currentType == CharacterType.SevenBone && sevenBoneObject != null && sevenBoneObject.activeSelf)
                return sevenBoneObject;
            if (currentType == CharacterType.ForgBone && forgBoneObject != null && forgBoneObject.activeSelf)
                return forgBoneObject;

            PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
            return pm != null ? pm.gameObject : null;
        }

        public GameObject GetCharacterObject(CharacterType type)
        {
            return type == CharacterType.SevenBone ? sevenBoneObject : forgBoneObject;
        }

        public void FindOrCreateCharacters()
        {
            // 1. Search existing in scene by name or component
            PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p == null) continue;
                string lower = p.gameObject.name.ToLower();
                if (lower.Contains("seven") && sevenBoneObject == null)
                {
                    sevenBoneObject = p.gameObject;
                }
                else if ((lower.Contains("forg") || lower.Contains("frog")) && forgBoneObject == null)
                {
                    forgBoneObject = p.gameObject;
                }
            }

            // If only one found and named generic, assign appropriately
            if (sevenBoneObject == null && players.Length > 0 && forgBoneObject != players[0].gameObject)
            {
                sevenBoneObject = players[0].gameObject;
                sevenBoneObject.name = "seven_bone";
            }

            // Ensure references
            EnsureCharacterInstance(CharacterType.SevenBone);
            EnsureCharacterInstance(CharacterType.ForgBone);
        }

        private void EnsureCharacterInstance(CharacterType type)
        {
            if (type == CharacterType.SevenBone && sevenBoneObject == null)
            {
                GameObject prefab = sevenBonePrefab;
#if UNITY_EDITOR
                if (prefab == null) prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(SevenPrefabPath);
#endif
                if (prefab != null)
                {
                    sevenBoneObject = Instantiate(prefab);
                    sevenBoneObject.name = "seven_bone";
                }
                else
                {
#if UNITY_EDITOR
                    GameObject fbx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/model/seven_bone.fbx");
                    if (fbx != null)
                    {
                        sevenBoneObject = SetupCharacterFromFbx(fbx, "seven_bone", 1.85f, 0.92f, 0.35f, "Assets/model/Materials/Meshy_AI_Demon_Marked_Warrior_1020062346_texture.mat");
                    }
#endif
                }
            }
            else if (type == CharacterType.ForgBone && forgBoneObject == null)
            {
                GameObject prefab = forgBonePrefab;
#if UNITY_EDITOR
                if (prefab == null) prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ForgPrefabPath);
#endif
                if (prefab != null)
                {
                    forgBoneObject = Instantiate(prefab);
                    forgBoneObject.name = "forg_bone";
                    forgBoneObject.SetActive(false);
                }
                else
                {
#if UNITY_EDITOR
                    GameObject fbx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/model/forg_bone.fbx");
                    if (fbx != null)
                    {
                        forgBoneObject = SetupCharacterFromFbx(fbx, "forg_bone", 1.1f, 0.55f, 0.45f, "Assets/model/Mat_Forg.mat");
                        if (forgBoneObject != null) forgBoneObject.SetActive(false);
                    }
#endif
                }
            }
        }

#if UNITY_EDITOR
        private GameObject SetupCharacterFromFbx(GameObject fbx, string name, float ccHeight, float ccCenterY, float ccRadius, string matPath)
        {
            GameObject go = Instantiate(fbx);
            go.name = name;
            go.transform.position = new Vector3(0, 0.15f, -7.5f);
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            // Load Avatar & Controller
            Avatar avatar = null;
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(fbx);
            foreach (var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (a is Avatar av) { avatar = av; break; }
            }

            RuntimeAnimatorController ctrl = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/hero_Animator.controller");

            Animator anim = go.GetComponent<Animator>();
            if (anim == null) anim = go.AddComponent<Animator>();
            anim.applyRootMotion = false;
            if (avatar != null) anim.avatar = avatar;
            if (ctrl != null) anim.runtimeAnimatorController = ctrl;

            // Material
            Material mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat != null)
            {
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterial = mat;
                }
            }

            // CharacterController
            CharacterController cc = go.GetComponent<CharacterController>();
            if (cc == null) cc = go.AddComponent<CharacterController>();
            cc.height = ccHeight;
            cc.center = new Vector3(0, ccCenterY, 0);
            cc.radius = ccRadius;
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 45f;
            cc.minMoveDistance = 0.001f;

            // PlayerMovement
            PlayerMovement pm = go.GetComponent<PlayerMovement>();
            if (pm == null) pm = go.AddComponent<PlayerMovement>();
            pm.walkSpeed = 5.0f;
            pm.runSpeed = 8.5f;
            pm.rotationSpeed = 720f;
            pm.jumpHeight = name.Contains("forg") ? 1.35f : 1.2f;
            pm.gravity = 20.0f;
            pm.rotationOffset = Vector3.zero;
            pm.animator = anim;
            if (Camera.main != null) pm.cameraTransform = Camera.main.transform;

            return go;
        }
#endif

        private bool ReadKeyDown(KeyCode legacyKey, Key newKey)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                var keyControl = kb[newKey];
                if (keyControl != null && keyControl.wasPressedThisFrame) return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(legacyKey)) return true;
#endif
            return false;
        }

        private bool ReadKeyDirect(Key newKey)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                var keyControl = kb[newKey];
                if (keyControl != null && keyControl.wasPressedThisFrame) return true;
            }
#endif
            return false;
        }

        private void InitGUIStyles()
        {
            if (stylesInitialized) return;

            windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.normal.background = MakeTex(2, 2, new Color(0.12f, 0.14f, 0.18f, 0.92f));
            windowStyle.onNormal.background = windowStyle.normal.background;

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            titleStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);

            buttonActiveStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            buttonActiveStyle.normal.textColor = Color.white;
            buttonActiveStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.65f, 0.32f, 0.95f));

            buttonNormalStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter
            };
            buttonNormalStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            buttonNormalStyle.normal.background = MakeTex(2, 2, new Color(0.24f, 0.26f, 0.32f, 0.9f));

            statusStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            statusStyle.normal.textColor = Color.cyan;

            tipStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter
            };
            tipStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);

            stylesInitialized = true;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void OnGUI()
        {
            if (!showOnScreenUI) return;

            InitGUIStyles();

            windowRect.height = isMinimized ? 55 : 220;
            windowRect = GUI.Window(9901, windowRect, DrawWindowContents, "🎮 主角切換 / Switcher", windowStyle);
        }

        private void DrawWindowContents(int windowID)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(isMinimized ? "主角切換 (已最小化)" : "選擇操作的主角", titleStyle);
            if (GUILayout.Button(isMinimized ? "▼ 展開" : "▲ 收合", GUILayout.Width(60), GUILayout.Height(18)))
            {
                isMinimized = !isMinimized;
            }
            GUILayout.EndHorizontal();

            if (isMinimized)
            {
                GUI.DragWindow();
                return;
            }

            GUILayout.Space(6);

            // Active character status
            string activeName = currentType == CharacterType.SevenBone ? "👤 seven_bone (戰士)" : "🐸 forg_bone (青蛙)";
            GUILayout.Label($"當前主角: {activeName}", statusStyle);

            GUILayout.Space(6);

            // 1. Switch to seven_bone button
            bool isSeven = currentType == CharacterType.SevenBone;
            if (GUILayout.Button(isSeven ? "▶ 👤 戰士 (seven_bone) [已選擇]" : "👤 戰士 (seven_bone) [按鍵 1]", isSeven ? buttonActiveStyle : buttonNormalStyle, GUILayout.Height(32)))
            {
                SwitchTo(CharacterType.SevenBone);
            }

            GUILayout.Space(4);

            // 2. Switch to forg_bone button
            bool isForg = currentType == CharacterType.ForgBone;
            if (GUILayout.Button(isForg ? "▶ 🐸 青蛙 (forg_bone) [已選擇]" : "🐸 青蛙 (forg_bone) [按鍵 2]", isForg ? buttonActiveStyle : buttonNormalStyle, GUILayout.Height(32)))
            {
                SwitchTo(CharacterType.ForgBone);
            }

            GUILayout.Space(8);

            // Quick Hotkey guide
            GUILayout.Label("• [Tab] 切換主角  • [1/2] 直接選擇", tipStyle);
            GUILayout.Label("• [WASD] 移動  • [Shift] 衝刺  • [Space] 跳躍", tipStyle);
            GUILayout.Label("• [F/V] 揮手動作  • [C] 隱藏/顯示視窗", tipStyle);

            GUI.DragWindow();
        }
    }
}
