#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PokemonArena
{
    public static class SevenBoneCharacterSetup
    {
        private const string SevenBoneFbxPath = "Assets/model/seven_bone.fbx";
        private const string IdleFbxPath = "Assets/Animations/Breathing Idle.fbx";
        private const string WalkFbxPath = "Assets/Animations/Walking.fbx";
        private const string RunFbxPath = "Assets/Animations/Run.fbx";
        private const string JumpFbxPath = "Assets/Animations/Jumping.fbx";
        private const string WaveFbxPath = "Assets/Animations/Waving.fbx";
        private const string ControllerPath = "Assets/Animations/hero_Animator.controller";
        private const string MaterialPath = "Assets/model/Materials/Meshy_AI_Demon_Marked_Warrior_1020062346_texture.mat";
        private const string AltMaterialPath = "Assets/model/Mat_FastRun_Warrior.mat";
        private const string PrefabPath = "Assets/model/seven_bone_Player.prefab";

        [InitializeOnLoadMethod]
        private static void OnEditorStartup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(PrefabPath))
                {
                    SetupSevenBonePlayerInCurrentScene(saveScene: true);
                }
            };
        }

        [MenuItem("Tools/Character/Setup Seven Bone Player", false, 1)]
        public static void SetupSevenBonePlayerMenu()
        {
            SetupSevenBonePlayerInCurrentScene(saveScene: true);
        }

        [MenuItem("Tools/Character/Setup Seven Bone Player in All Scenes", false, 2)]
        public static void SetupAllScenesMenu()
        {
            SetupAllScenes();
        }

        public static void SetupAllScenes()
        {
            Scene activeScene = EditorSceneManager.GetActiveScene();
            string originalPath = activeScene.path;
            string[] scenes = new string[] { "Assets/Scenes/SampleScene.unity", "Assets/Scenes/pokemon.unity" };
            foreach (var scPath in scenes)
            {
                if (File.Exists(scPath))
                {
                    EditorSceneManager.OpenScene(scPath, OpenSceneMode.Single);
                    SetupSevenBonePlayerInCurrentScene(saveScene: true);
                }
            }
            if (!string.IsNullOrEmpty(originalPath) && File.Exists(originalPath))
            {
                EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
            }
        }

        public static GameObject SetupSevenBonePlayerInCurrentScene(bool saveScene = true)
        {
            Debug.Log("<color=#2196F3><b>[SevenBone Setup]</b> Starting full setup for seven_bone with Idle, Walk, Run, Jump, Wave...</color>");

            // 1. Configure Animation FBXs (Humanoid, Looping, Root Transform Bake)
            ConfigureAnimationFbx(IdleFbxPath, isLooping: true);
            ConfigureAnimationFbx(WalkFbxPath, isLooping: true);
            ConfigureAnimationFbx(RunFbxPath, isLooping: true);
            ConfigureAnimationFbx(JumpFbxPath, isLooping: false);
            ConfigureAnimationFbx(WaveFbxPath, isLooping: false);

            // 2. Configure seven_bone FBX (Humanoid rig & avatar)
            ConfigureModelFbx(SevenBoneFbxPath);

            // 3. Ensure hero_Animator controller is fully configured
            AnimatorController controller = EnsureHeroAnimatorController();

            // 4. Remove previous players if any
            PlayerMovement[] existing = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
            foreach (var p in existing)
            {
                if (p != null && p.gameObject != null)
                {
                    Undo.DestroyObjectImmediate(p.gameObject);
                }
            }

            // 5. Load seven_bone FBX
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(SevenBoneFbxPath);
            if (fbxAsset == null)
            {
                Debug.LogError($"[SevenBone Setup] Cannot load FBX at {SevenBoneFbxPath}");
                return null;
            }

            // 6. Instantiate into current scene
            GameObject playerGo = PrefabUtility.InstantiatePrefab(fbxAsset) as GameObject;
            if (playerGo == null) playerGo = Object.Instantiate(fbxAsset);
            playerGo.name = "seven_bone";
            playerGo.transform.position = new Vector3(0, 0.15f, -7.5f);
            playerGo.transform.rotation = Quaternion.identity;
            playerGo.transform.localScale = Vector3.one;

            // 7. Find Humanoid Avatar from seven_bone.fbx
            Avatar avatar = null;
            Object[] allSevenBoneAssets = AssetDatabase.LoadAllAssetsAtPath(SevenBoneFbxPath);
            foreach (var a in allSevenBoneAssets)
            {
                if (a is Avatar av)
                {
                    avatar = av;
                    break;
                }
            }

            // 8. Configure Animator
            Animator anim = playerGo.GetComponent<Animator>();
            if (anim == null) anim = playerGo.AddComponent<Animator>();
            anim.applyRootMotion = false;
            if (avatar != null) anim.avatar = avatar;
            if (controller != null) anim.runtimeAnimatorController = controller;

            // 9. Assign Material
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null) mat = AssetDatabase.LoadAssetAtPath<Material>(AltMaterialPath);
            if (mat != null)
            {
                Renderer[] renderers = playerGo.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    r.sharedMaterial = mat;
                }
            }

            // 10. Configure CharacterController
            CharacterController cc = playerGo.GetComponent<CharacterController>();
            if (cc == null) cc = playerGo.AddComponent<CharacterController>();
            cc.height = 1.85f;
            cc.center = new Vector3(0, 0.92f, 0);
            cc.radius = 0.35f;
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 45f;
            cc.minMoveDistance = 0.001f;

            // 11. Configure PlayerMovement
            PlayerMovement pm = playerGo.GetComponent<PlayerMovement>();
            if (pm == null) pm = playerGo.AddComponent<PlayerMovement>();
            pm.walkSpeed = 5.0f;
            pm.runSpeed = 8.5f;
            pm.rotationSpeed = 720f;
            pm.jumpHeight = 1.2f;
            pm.gravity = 20.0f;
            pm.rotationOffset = Vector3.zero;
            pm.animator = anim;

            // 12. Link Main Camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                pm.cameraTransform = mainCam.transform;
                ArenaCameraController camCtrl = mainCam.GetComponent<ArenaCameraController>();
                if (camCtrl == null) camCtrl = mainCam.gameObject.AddComponent<ArenaCameraController>();
                camCtrl.playerTarget = playerGo.transform;
                camCtrl.SetMode(ArenaCameraController.CameraMode.FollowPlayer, immediate: true);
            }

            // 13. Save as reusable Prefab
            PrefabUtility.SaveAsPrefabAssetAndConnect(playerGo, PrefabPath, InteractionMode.AutomatedAction);
            Debug.Log($"<color=#4CAF50><b>[SevenBone Setup]</b> Saved player prefab to {PrefabPath}</color>");

            Undo.RegisterCreatedObjectUndo(playerGo, "Add seven_bone Player");
            Selection.activeGameObject = playerGo;

            if (saveScene)
            {
                Scene activeScene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
            }

            Debug.Log("<color=#4CAF50><b>[SevenBone Setup]</b> seven_bone Player successfully set up with 5 animations in scene: " + EditorSceneManager.GetActiveScene().name + "!</color>");
            return playerGo;
        }

        private static void ConfigureModelFbx(string path)
        {
            ModelImporter mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) return;

            bool modified = false;
            if (mi.animationType != ModelImporterAnimationType.Human)
            {
                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                modified = true;
            }

            if (modified)
            {
                mi.SaveAndReimport();
            }
        }

        private static void ConfigureAnimationFbx(string path, bool isLooping)
        {
            ModelImporter mi = AssetImporter.GetAtPath(path) as ModelImporter;
            if (mi == null) return;

            bool modified = false;
            if (mi.animationType != ModelImporterAnimationType.Human)
            {
                mi.animationType = ModelImporterAnimationType.Human;
                mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                modified = true;
            }

            ModelImporterClipAnimation[] clips = mi.defaultClipAnimations;
            if (clips == null || clips.Length == 0) clips = mi.clipAnimations;

            if (clips != null && clips.Length > 0)
            {
                foreach (var c in clips)
                {
                    c.loopTime = isLooping;
                    c.loopPose = isLooping;
                    c.lockRootRotation = true;
                    c.lockRootHeightY = true;
                    c.lockRootPositionXZ = true;
                    c.keepOriginalOrientation = true;
                    c.keepOriginalPositionY = true;
                    c.keepOriginalPositionXZ = true;
                    modified = true;
                }
                mi.clipAnimations = clips;
            }

            if (modified)
            {
                mi.SaveAndReimport();
            }
        }

        public static AnimatorController EnsureHeroAnimatorController()
        {
            AnimatorController ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (ctrl == null)
            {
                ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            }

            // Ensure parameters
            EnsureParameter(ctrl, "IsWalking", AnimatorControllerParameterType.Bool);
            EnsureParameter(ctrl, "IsRunning", AnimatorControllerParameterType.Bool);
            EnsureParameter(ctrl, "IsJumping", AnimatorControllerParameterType.Bool);
            EnsureParameter(ctrl, "IsWaving", AnimatorControllerParameterType.Bool);
            EnsureParameter(ctrl, "Speed", AnimatorControllerParameterType.Float);

            // Load clips
            AnimationClip idleClip = LoadFirstClip(IdleFbxPath);
            AnimationClip walkClip = LoadFirstClip(WalkFbxPath);
            AnimationClip runClip = LoadFirstClip(RunFbxPath);
            AnimationClip jumpClip = LoadFirstClip(JumpFbxPath);
            AnimationClip waveClip = LoadFirstClip(WaveFbxPath);

            AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

            AnimatorState idleState = GetOrCreateState(sm, "Idle", idleClip);
            AnimatorState walkState = GetOrCreateState(sm, "walking", walkClip);
            AnimatorState runState = GetOrCreateState(sm, "Running", runClip);
            AnimatorState jumpState = GetOrCreateState(sm, "Jumping", jumpClip);
            AnimatorState waveState = GetOrCreateState(sm, "Waving", waveClip);

            sm.defaultState = idleState;
            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            return ctrl;
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
