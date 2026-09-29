#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

namespace PokemonArena
{
    [InitializeOnLoad]
    [CustomEditor(typeof(PokemonArenaBuilder))]
    public class PokemonArenaEditor : Editor
    {
        private const string PrefabFolder = "Assets/PokemonArena/Prefabs";
        private const string PrefabPath = "Assets/PokemonArena/Prefabs/PokemonBattleArena.prefab";
        private const string SessionKey = "PokemonArena_Initialized_Chara2_Once";

        static PokemonArenaEditor()
        {
            EditorApplication.delayCall += InitializeOnStartup;
        }

        private static void InitializeOnStartup()
        {
            PokemonArenaBuilder existingArena = Object.FindFirstObjectByType<PokemonArenaBuilder>();
            if (existingArena == null)
            {
                SetupFullSceneWithCharacter();
            }
            else
            {
                PlayerMovement existingPlayer = Object.FindFirstObjectByType<PlayerMovement>();
                if (existingPlayer == null)
                {
                    Debug.Log("<color=#4CAF50><b>[Pokemon Arena]</b> Adding Chara2 Player with WASD controls to scene...</color>");
                    AddChara2ToScene(saveScene: true);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            PokemonArenaBuilder builder = (PokemonArenaBuilder)target;

            // Header Banner
            EditorGUILayout.Space(8);
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("⚡ Pokémon Battle Arena Builder ⚡", headerStyle);
            EditorGUILayout.HelpBox("Closed battleground surrounded by fences, complete with Pokéball pitch markings, trainer podiums, stadium floodlights, and camera controls.", MessageType.Info);
            EditorGUILayout.Space(6);

            // Default properties
            DrawDefaultInspector();

            EditorGUILayout.Space(12);

            // Main Actions
            GUI.backgroundColor = new Color(0.3f, 0.85f, 0.35f);
            if (GUILayout.Button("⚡ Generate / Rebuild Arena", GUILayout.Height(36)))
            {
                Undo.RegisterFullObjectHierarchyUndo(builder.gameObject, "Generate Pokemon Arena");
                builder.BuildArena();
                EditorUtility.SetDirty(builder.gameObject);
                EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
                SaveArenaAsPrefab(builder.gameObject);
            }

            GUI.backgroundColor = new Color(0.2f, 0.7f, 1.0f);
            if (GUILayout.Button("🎮 Add / Respawn Chara2 Player (WASD)", GUILayout.Height(32)))
            {
                AddChara2ToScene(saveScene: true);
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🎨 Regenerate Textures", GUILayout.Height(28)))
            {
                ArenaTextureGenerator.GenerateAllTextures(forceOverwrite: true);
                builder.BuildArena();
                EditorUtility.SetDirty(builder.gameObject);
            }

            if (GUILayout.Button("💾 Save As Prefab", GUILayout.Height(28)))
            {
                SaveArenaAsPrefab(builder.gameObject);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            GUI.backgroundColor = new Color(1.0f, 0.45f, 0.45f);
            if (GUILayout.Button("🗑 Clear Arena", GUILayout.Height(26)))
            {
                if (EditorUtility.DisplayDialog("Clear Arena", "Are you sure you want to remove all generated arena objects?", "Clear", "Cancel"))
                {
                    Undo.RegisterFullObjectHierarchyUndo(builder.gameObject, "Clear Pokemon Arena");
                    builder.ClearArena();
                    EditorUtility.SetDirty(builder.gameObject);
                    EditorSceneManager.MarkSceneDirty(builder.gameObject.scene);
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.Space(8);
        }

        [MenuItem("Tools/Pokemon Arena/⚡ Generate Arena in Scene", false, 1)]
        public static void GenerateArenaInSceneMenu()
        {
            GenerateArenaInSceneInternal(saveSceneAndPrefab: true);
        }

        [MenuItem("Tools/Pokemon Arena/🎮 Add Playable Chara2 to Scene (WASD)", false, 2)]
        public static void AddChara2Menu()
        {
            AddChara2ToScene(saveScene: true);
        }

        [MenuItem("Tools/Pokemon Arena/🚀 Setup Full Arena + Chara2 Scene", false, 0)]
        public static void SetupFullSceneWithCharacter()
        {
            GameObject arenaGo = GenerateArenaInSceneInternal(saveSceneAndPrefab: true);
            AddChara2ToScene(saveScene: true);
            Debug.Log("<color=#4CAF50><b>[Pokemon Arena]</b> Full Scene with Battle Arena and Chara2 Player successfully set up!</color>");
        }

        public static GameObject AddChara2ToScene(bool saveScene)
        {
            // Remove existing Chara2 if any
            PlayerMovement existingPlayer = Object.FindFirstObjectByType<PlayerMovement>();
            if (existingPlayer != null)
            {
                Undo.DestroyObjectImmediate(existingPlayer.gameObject);
            }

            // Find FBX
            string[] possiblePaths = new string[]
            {
                "Assets/model/chara2.fbx",
                "Assets/model/charach.fbx"
            };

            GameObject fbxPrefab = null;
            foreach (var p in possiblePaths)
            {
                fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                if (fbxPrefab != null) break;
            }

            if (fbxPrefab == null)
            {
                Debug.LogError("[Pokemon Arena] Could not locate chara2.fbx or charach.fbx in Assets/model!");
                return null;
            }

            // Instantiate
            GameObject charaInstance = PrefabUtility.InstantiatePrefab(fbxPrefab) as GameObject;
            charaInstance.name = "Chara2_Player";
            charaInstance.transform.position = new Vector3(0, 0.15f, -7.5f); // Trainer 1 active battle circle
            charaInstance.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // X-axis rotated -90 degrees

            // Add or configure CharacterController
            CharacterController cc = charaInstance.GetComponent<CharacterController>();
            if (cc == null) cc = charaInstance.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.radius = 0.35f;
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 45f;
            cc.minMoveDistance = 0.001f;

            // Add PlayerMovement
            PlayerMovement pm = charaInstance.GetComponent<PlayerMovement>();
            if (pm == null) pm = charaInstance.AddComponent<PlayerMovement>();
            pm.walkSpeed = 5.0f;
            pm.runSpeed = 8.5f;
            pm.jumpHeight = 1.2f;

            // Configure Camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                pm.cameraTransform = mainCam.transform;
                ArenaCameraController camCtrl = mainCam.GetComponent<ArenaCameraController>();
                if (camCtrl == null) camCtrl = mainCam.gameObject.AddComponent<ArenaCameraController>();
                camCtrl.playerTarget = charaInstance.transform;
                camCtrl.SetMode(ArenaCameraController.CameraMode.FollowPlayer, immediate: true);
            }

            Undo.RegisterCreatedObjectUndo(charaInstance, "Add Chara2 Player");
            Selection.activeGameObject = charaInstance;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }

            if (saveScene)
            {
                var activeScene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
            }

            Debug.Log("<color=#4CAF50><b>[Pokemon Arena]</b> Chara2 added to scene with CharacterController and WASD PlayerMovement!</color>");
            return charaInstance;
        }

        private static GameObject GenerateArenaInSceneInternal(bool saveSceneAndPrefab)
        {
            PokemonArenaBuilder existing = Object.FindFirstObjectByType<PokemonArenaBuilder>();
            GameObject arenaGo;
            if (existing != null)
            {
                arenaGo = existing.gameObject;
            }
            else
            {
                arenaGo = new GameObject("Pokemon_Battle_Arena");
                existing = arenaGo.AddComponent<PokemonArenaBuilder>();
            }

            Undo.RegisterCreatedObjectUndo(arenaGo, "Create Pokemon Arena");
            existing.BuildArena();

            Selection.activeGameObject = arenaGo;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }

            if (saveSceneAndPrefab)
            {
                SaveArenaAsPrefab(arenaGo);
                var activeScene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
            }

            Debug.Log("<color=#4CAF50><b>[Pokemon Arena]</b> Arena generated and saved successfully!</color>");
            return arenaGo;
        }

        [MenuItem("Tools/Pokemon Arena/🗑 Clear Arena in Scene", false, 20)]
        public static void ClearArenaInSceneMenu()
        {
            PokemonArenaBuilder existing = Object.FindFirstObjectByType<PokemonArenaBuilder>();
            if (existing != null)
            {
                existing.ClearArena();
                var activeScene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log("<color=#FF9800><b>[Pokemon Arena]</b> Arena cleared.</color>");
            }
            else
            {
                Debug.LogWarning("[Pokemon Arena] No PokemonArenaBuilder found in scene.");
            }
        }

        [MenuItem("Tools/Pokemon Arena/🎨 Regenerate All Textures", false, 30)]
        public static void RegenerateTexturesMenu()
        {
            ArenaTextureGenerator.GenerateAllTextures(forceOverwrite: true);
            Debug.Log("<color=#2196F3><b>[Pokemon Arena]</b> High-res textures regenerated!</color>");
        }

        [MenuItem("Tools/Pokemon Arena/💾 Save Arena as Prefab", false, 31)]
        public static void SavePrefabMenu()
        {
            PokemonArenaBuilder existing = Object.FindFirstObjectByType<PokemonArenaBuilder>();
            if (existing != null)
            {
                SaveArenaAsPrefab(existing.gameObject);
                EditorUtility.DisplayDialog("Prefab Saved", $"Arena prefab saved successfully at:\n{PrefabPath}", "Great!");
            }
            else
            {
                EditorUtility.DisplayDialog("Save Prefab", "No PokemonArenaBuilder found in the current scene. Please generate the arena first.", "OK");
            }
        }

        public static void SaveArenaAsPrefab(GameObject arenaGo)
        {
            if (!Directory.Exists(PrefabFolder))
            {
                Directory.CreateDirectory(PrefabFolder);
            }

            bool success;
            PrefabUtility.SaveAsPrefabAssetAndConnect(arenaGo, PrefabPath, InteractionMode.UserAction, out success);
            if (success)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"<color=#4CAF50><b>[Pokemon Arena]</b> Prefab successfully saved at: {PrefabPath}</color>");
            }
            else
            {
                Debug.LogError("[Pokemon Arena] Failed to save prefab.");
            }
        }
    }
}
#endif
