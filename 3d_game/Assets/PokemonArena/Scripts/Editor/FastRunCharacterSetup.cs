#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PokemonArena
{
    [InitializeOnLoad]
    public static class FastRunCharacterSetup
    {
        private const string FbxPath = "Assets/model/Fast Run.fbx";
        private const string ControllerPath = "Assets/model/FastRun_AnimatorController.controller";
        private const string MaterialPath = "Assets/model/Mat_FastRun_Warrior.mat";
        private const string TexturePath = "Assets/model/Meshy_AI_Demon_Marked_Warrior_1020062346_texture.png";
        private const string NormalTexturePath = "Assets/model/Meshy_AI_Demon_Marked_Warrior_1020062346_texture_normal.png";

        static FastRunCharacterSetup()
        {
            EditorApplication.delayCall += OnEditorStartup;
        }

        private static void OnEditorStartup()
        {
            PlayerMovement pm = Object.FindFirstObjectByType<PlayerMovement>();
            if (pm == null || pm.gameObject.name != "FastRun_Player")
            {
                Debug.Log("<color=#2196F3><b>[Fast Run Setup]</b> Initializing Fast Run FBX model and player...</color>");
                SwitchToFastRunInCurrentScene(saveScene: true);
            }
        }

        [MenuItem("Tools/Pokemon Arena/🏃 Switch Player to Fast Run FBX (Current Scene)", false, 1)]
        public static void SwitchToFastRunMenu()
        {
            SwitchToFastRunInCurrentScene(saveScene: true);
        }

        [MenuItem("Tools/Pokemon Arena/🚀 Setup Fast Run Player in All Scenes", false, 2)]
        public static void SetupAllScenesMenu()
        {
            SetupAllScenes();
        }

        public static void SetupAllScenes()
        {
            ConfigureModelImporter();
            EnsureMaterial();
            EnsureAnimatorController();

            Scene activeScene = EditorSceneManager.GetActiveScene();
            string originalPath = activeScene.path;

            string[] scenes = new string[]
            {
                "Assets/Scenes/SampleScene.unity",
                "Assets/Scenes/pokemon.unity"
            };

            foreach (var scPath in scenes)
            {
                if (File.Exists(scPath))
                {
                    EditorSceneManager.OpenScene(scPath, OpenSceneMode.Single);
                    SetupPlayerInActiveScene(saveScene: true);
                }
            }

            if (!string.IsNullOrEmpty(originalPath) && File.Exists(originalPath))
            {
                EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
            }

            Debug.Log("<color=#4CAF50><b>[Fast Run Setup]</b> All scenes configured with FastRun_Player!</color>");
        }

        public static GameObject SwitchToFastRunInCurrentScene(bool saveScene = true)
        {
            ConfigureModelImporter();
            EnsureMaterial();
            EnsureAnimatorController();
            return SetupPlayerInActiveScene(saveScene);
        }

        public static void ConfigureModelImporter()
        {
            ModelImporter mi = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
            if (mi == null)
            {
                Debug.LogError($"[Fast Run Setup] ModelImporter not found at {FbxPath}");
                return;
            }

            bool needsReimport = false;

            if (mi.useFileUnits || mi.useFileScale || mi.globalScale != 1.0f)
            {
                mi.useFileUnits = false;
                mi.useFileScale = false;
                mi.globalScale = 1.0f;
                needsReimport = true;
            }

            if (mi.animationType != ModelImporterAnimationType.Human || mi.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                needsReimport = true;
            }

            // Configure Run and Idle clips
            ModelImporterClipAnimation[] existingClips = mi.clipAnimations;
            bool hasRun = false;
            bool hasIdle = false;

            if (existingClips != null)
            {
                foreach (var c in existingClips)
                {
                    if (c.name == "Run") hasRun = true;
                    if (c.name == "Idle") hasIdle = true;
                }
            }

            if (!hasRun || !hasIdle || existingClips == null || existingClips.Length == 0)
            {
                var runClip = new ModelImporterClipAnimation
                {
                    name = "Run",
                    takeName = "mixamo.com",
                    firstFrame = 0,
                    lastFrame = 16,
                    loopTime = true,
                    loopPose = true,
                    lockRootPositionXZ = true,
                    lockRootHeightY = true,
                    lockRootRotation = true
                };

                var idleClip = new ModelImporterClipAnimation
                {
                    name = "Idle",
                    takeName = "mixamo.com",
                    firstFrame = 0,
                    lastFrame = 1,
                    loopTime = true,
                    loopPose = false,
                    lockRootPositionXZ = true,
                    lockRootHeightY = true,
                    lockRootRotation = true
                };

                mi.clipAnimations = new ModelImporterClipAnimation[] { idleClip, runClip };
                needsReimport = true;
            }

            if (needsReimport)
            {
                mi.SaveAndReimport();
                Debug.Log("<color=#4CAF50><b>[Fast Run Setup]</b> Fast Run.fbx ModelImporter reconfigured & reimported.</color>");
            }
        }

        public static Material EnsureMaterial()
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);

                Texture2D baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
                if (baseTex != null)
                {
                    if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", baseTex);
                    if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", baseTex);
                }

                Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalTexturePath);
                if (normalTex != null)
                {
                    if (mat.HasProperty("_BumpMap"))
                    {
                        mat.SetTexture("_BumpMap", normalTex);
                        mat.EnableKeyword("_NORMALMAP");
                    }
                }

                AssetDatabase.CreateAsset(mat, MaterialPath);
                AssetDatabase.SaveAssets();
                Debug.Log("<color=#4CAF50><b>[Fast Run Setup]</b> Created material at " + MaterialPath + "</color>");
            }
            return mat;
        }

        public static AnimatorController EnsureAnimatorController()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            // Ensure parameters
            EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "IsMoving", AnimatorControllerParameterType.Bool);

            // Find Run and Idle clips
            AnimationClip runClip = null;
            AnimationClip idleClip = null;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(FbxPath);
            foreach (var a in assets)
            {
                if (a is AnimationClip clip)
                {
                    if (clip.name == "Run") runClip = clip;
                    else if (clip.name == "Idle") idleClip = clip;
                    else if (clip.name == "mixamo.com" && runClip == null) runClip = clip;
                }
            }

            if (runClip == null && idleClip != null) runClip = idleClip;
            if (idleClip == null && runClip != null) idleClip = runClip;

            AnimatorStateMachine sm = controller.layers[0].stateMachine;

            AnimatorState idleState = null;
            AnimatorState runState = null;

            foreach (var childState in sm.states)
            {
                if (childState.state.name == "Idle") idleState = childState.state;
                if (childState.state.name == "Run") runState = childState.state;
            }

            if (idleState == null)
            {
                idleState = sm.AddState("Idle");
            }
            idleState.motion = idleClip;

            if (runState == null)
            {
                runState = sm.AddState("Run");
            }
            runState.motion = runClip;

            // Idle -> Run transition
            bool hasIdleToRun = false;
            foreach (var t in idleState.transitions)
            {
                if (t.destinationState == runState) { hasIdleToRun = true; break; }
            }
            if (!hasIdleToRun)
            {
                var trans = idleState.AddTransition(runState);
                trans.hasExitTime = false;
                trans.duration = 0.15f;
                trans.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            }

            // Run -> Idle transition
            bool hasRunToIdle = false;
            foreach (var t in runState.transitions)
            {
                if (t.destinationState == idleState) { hasRunToIdle = true; break; }
            }
            if (!hasRunToIdle)
            {
                var trans = runState.AddTransition(idleState);
                trans.hasExitTime = false;
                trans.duration = 0.2f;
                trans.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            }

            sm.defaultState = idleState;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static void EnsureParameter(AnimatorController controller, string paramName, AnimatorControllerParameterType type)
        {
            foreach (var p in controller.parameters)
            {
                if (p.name == paramName) return;
            }
            controller.AddParameter(paramName, type);
        }

        public static GameObject SetupPlayerInActiveScene(bool saveScene)
        {
            // 1. Remove existing PlayerMovement GameObjects
            PlayerMovement[] existing = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
            foreach (var p in existing)
            {
                if (p != null && p.gameObject != null)
                {
                    Undo.DestroyObjectImmediate(p.gameObject);
                }
            }

            // 2. Load FBX Prefab
            GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
            if (fbxPrefab == null)
            {
                Debug.LogError($"[Fast Run Setup] Cannot load FBX at {FbxPath}");
                return null;
            }

            // 3. Instantiate
            GameObject playerGo = PrefabUtility.InstantiatePrefab(fbxPrefab) as GameObject;
            if (playerGo == null) playerGo = Object.Instantiate(fbxPrefab);
            playerGo.name = "FastRun_Player";
            playerGo.transform.position = new Vector3(0, 0.15f, -7.5f);
            playerGo.transform.rotation = Quaternion.identity;
            playerGo.transform.localScale = Vector3.one;

            // 4. Apply Material
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat != null)
            {
                Renderer[] renderers = playerGo.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    r.sharedMaterial = mat;
                }
            }

            // 5. Setup CharacterController
            CharacterController cc = playerGo.GetComponent<CharacterController>();
            if (cc == null) cc = playerGo.AddComponent<CharacterController>();
            cc.height = 1.85f;
            cc.center = new Vector3(0, 0.92f, 0);
            cc.radius = 0.35f;
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 45f;
            cc.minMoveDistance = 0.001f;

            // 6. Setup Animator
            Animator anim = playerGo.GetComponent<Animator>();
            if (anim == null) anim = playerGo.AddComponent<Animator>();
            anim.applyRootMotion = false;
            RuntimeAnimatorController rac = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            if (rac != null) anim.runtimeAnimatorController = rac;

            // 7. Setup PlayerMovement
            PlayerMovement pm = playerGo.GetComponent<PlayerMovement>();
            if (pm == null) pm = playerGo.AddComponent<PlayerMovement>();
            pm.walkSpeed = 5.0f;
            pm.runSpeed = 8.5f;
            pm.rotationSpeed = 720f;
            pm.jumpHeight = 1.2f;
            pm.gravity = 20.0f;
            pm.rotationOffset = Vector3.zero;
            pm.animator = anim;

            // 8. Setup Camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                pm.cameraTransform = mainCam.transform;
                ArenaCameraController camCtrl = mainCam.GetComponent<ArenaCameraController>();
                if (camCtrl == null) camCtrl = mainCam.gameObject.AddComponent<ArenaCameraController>();
                camCtrl.playerTarget = playerGo.transform;
                camCtrl.SetMode(ArenaCameraController.CameraMode.FollowPlayer, immediate: true);
            }

            Undo.RegisterCreatedObjectUndo(playerGo, "Add FastRun Player");
            Selection.activeGameObject = playerGo;
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }

            if (saveScene)
            {
                Scene activeScene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
            }

            Debug.Log("<color=#4CAF50><b>[Fast Run Setup]</b> FastRun_Player successfully created and linked to Camera in scene: " + EditorSceneManager.GetActiveScene().name + "!</color>");
            return playerGo;
        }
    }
}
#endif
