#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PokemonArena
{
    public static class ForgBoneCharacterSetup
    {
        public const string ForgBoneFbxPath = "Assets/model/forg_bone.fbx";
        public const string IdleFbxPath = "Assets/Animations/Breathing Idle.fbx";
        public const string WalkFbxPath = "Assets/Animations/Walking.fbx";
        public const string RunFbxPath = "Assets/Animations/Run.fbx";
        public const string JumpFbxPath = "Assets/Animations/Jumping.fbx";
        public const string WaveFbxPath = "Assets/Animations/Waving.fbx";
        public const string ControllerPath = "Assets/Animations/hero_Animator.controller";
        public const string MaterialPath = "Assets/model/Mat_Forg.mat";
        public const string PrefabPath = "Assets/model/forg_bone_Player.prefab";

        [MenuItem("Tools/Character/Setup Forg Bone Player", false, 11)]
        public static void SetupForgBonePlayerMenu()
        {
            SetupForgBonePlayerInCurrentScene(saveScene: true);
        }

        [MenuItem("Tools/Character/Setup Forg Bone Player in All Scenes", false, 12)]
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
                    SetupForgBonePlayerInCurrentScene(saveScene: true);
                }
            }
            if (!string.IsNullOrEmpty(originalPath) && File.Exists(originalPath))
            {
                EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);
            }
        }

        public static GameObject SetupForgBonePlayerInCurrentScene(bool saveScene = true)
        {
            Debug.Log("<color=#4CAF50><b>[ForgBone Setup]</b> Starting full setup for forg_bone with Idle, Walk, Run, Jump, Wave...</color>");

            // 1. Configure Animation FBXs (Humanoid, Looping, Root Transform Bake)
            ConfigureAnimationFbx(IdleFbxPath, isLooping: true);
            ConfigureAnimationFbx(WalkFbxPath, isLooping: true);
            ConfigureAnimationFbx(RunFbxPath, isLooping: true);
            ConfigureAnimationFbx(JumpFbxPath, isLooping: false);
            ConfigureAnimationFbx(WaveFbxPath, isLooping: false);

            // 2. Configure forg_bone FBX (Humanoid rig & avatar)
            ConfigureModelFbx(ForgBoneFbxPath);

            // 3. Ensure hero_Animator controller is fully configured
            AnimatorController controller = SevenBoneCharacterSetup.EnsureHeroAnimatorController();

            // 4. Remove previous players if any (or keep if switch manager is setting up)
            PlayerMovement[] existing = Object.FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
            foreach (var p in existing)
            {
                if (p != null && p.gameObject != null)
                {
                    Undo.DestroyObjectImmediate(p.gameObject);
                }
            }

            // 5. Load forg_bone FBX
            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ForgBoneFbxPath);
            if (fbxAsset == null)
            {
                Debug.LogError($"[ForgBone Setup] Cannot load FBX at {ForgBoneFbxPath}");
                return null;
            }

            // 6. Instantiate into current scene
            GameObject playerGo = PrefabUtility.InstantiatePrefab(fbxAsset) as GameObject;
            if (playerGo == null) playerGo = Object.Instantiate(fbxAsset);
            playerGo.name = "forg_bone";
            playerGo.transform.position = new Vector3(0, 0.15f, -7.5f);
            playerGo.transform.rotation = Quaternion.identity;
            playerGo.transform.localScale = Vector3.one;

            // 7. Find Humanoid Avatar from forg_bone.fbx
            Avatar avatar = null;
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(ForgBoneFbxPath);
            foreach (var a in allAssets)
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
            if (mat != null)
            {
                Renderer[] renderers = playerGo.GetComponentsInChildren<Renderer>(true);
                foreach (var r in renderers)
                {
                    r.sharedMaterial = mat;
                }
            }

            // 10. Configure CharacterController (Tailored for Frog shape)
            CharacterController cc = playerGo.GetComponent<CharacterController>();
            if (cc == null) cc = playerGo.AddComponent<CharacterController>();
            cc.height = 1.1f;
            cc.center = new Vector3(0, 0.55f, 0);
            cc.radius = 0.45f;
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 45f;
            cc.minMoveDistance = 0.001f;

            // 11. Configure PlayerMovement
            PlayerMovement pm = playerGo.GetComponent<PlayerMovement>();
            if (pm == null) pm = playerGo.AddComponent<PlayerMovement>();
            pm.walkSpeed = 5.0f;
            pm.runSpeed = 8.5f;
            pm.rotationSpeed = 720f;
            pm.jumpHeight = 1.35f;
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
            Debug.Log($"<color=#4CAF50><b>[ForgBone Setup]</b> Saved frog player prefab to {PrefabPath}</color>");

            Undo.RegisterCreatedObjectUndo(playerGo, "Add forg_bone Player");
            Selection.activeGameObject = playerGo;

            if (saveScene)
            {
                Scene activeScene = EditorSceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(activeScene);
                EditorSceneManager.SaveScene(activeScene);
            }

            Debug.Log("<color=#4CAF50><b>[ForgBone Setup]</b> forg_bone Player successfully set up with 5 animations in scene: " + EditorSceneManager.GetActiveScene().name + "!</color>");
            return playerGo;
        }

        public static GameObject CreateOrUpdateForgBonePrefabOnly()
        {
            ConfigureModelFbx(ForgBoneFbxPath);
            AnimatorController controller = SevenBoneCharacterSetup.EnsureHeroAnimatorController();

            GameObject fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ForgBoneFbxPath);
            if (fbxAsset == null) return null;

            GameObject tempGo = Object.Instantiate(fbxAsset);
            tempGo.name = "forg_bone";
            tempGo.transform.position = Vector3.zero;
            tempGo.transform.rotation = Quaternion.identity;
            tempGo.transform.localScale = Vector3.one;

            Avatar avatar = null;
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(ForgBoneFbxPath);
            foreach (var a in allAssets)
            {
                if (a is Avatar av) { avatar = av; break; }
            }

            Animator anim = tempGo.GetComponent<Animator>();
            if (anim == null) anim = tempGo.AddComponent<Animator>();
            anim.applyRootMotion = false;
            if (avatar != null) anim.avatar = avatar;
            if (controller != null) anim.runtimeAnimatorController = controller;

            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat != null)
            {
                foreach (var r in tempGo.GetComponentsInChildren<Renderer>(true))
                {
                    r.sharedMaterial = mat;
                }
            }

            CharacterController cc = tempGo.GetComponent<CharacterController>();
            if (cc == null) cc = tempGo.AddComponent<CharacterController>();
            cc.height = 1.1f;
            cc.center = new Vector3(0, 0.55f, 0);
            cc.radius = 0.45f;
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 45f;
            cc.minMoveDistance = 0.001f;

            PlayerMovement pm = tempGo.GetComponent<PlayerMovement>();
            if (pm == null) pm = tempGo.AddComponent<PlayerMovement>();
            pm.walkSpeed = 5.0f;
            pm.runSpeed = 8.5f;
            pm.rotationSpeed = 720f;
            pm.jumpHeight = 1.35f;
            pm.gravity = 20.0f;
            pm.rotationOffset = Vector3.zero;
            pm.animator = anim;

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempGo, PrefabPath);
            Object.DestroyImmediate(tempGo);
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=#4CAF50><b>[ForgBone Setup]</b> Saved/Updated frog prefab at {PrefabPath}</color>");
            return savedPrefab;
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
    }
}
#endif
