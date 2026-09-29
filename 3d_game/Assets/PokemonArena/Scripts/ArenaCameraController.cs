using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PokemonArena
{
    public class ArenaCameraController : MonoBehaviour
    {
        public enum CameraMode
        {
            FollowPlayer,
            Broadcast,
            Trainer1,
            Trainer2,
            BattleCenter,
            CinematicOrbit,
            FreeLook
        }

        [Header("Target & Mode")]
        public Transform playerTarget;
        public Transform arenaCenter;
        public CameraMode currentMode = CameraMode.FollowPlayer;

        [Header("Smoothing")]
        public float positionSmoothSpeed = 6.0f;
        public float rotationSmoothSpeed = 6.0f;

        [Header("Follow Settings")]
        public float followDistance = 6.0f;
        public float followHeight = 3.0f;

        [Header("Orbit Settings")]
        public float orbitDistance = 34.0f;
        public float orbitHeight = 16.0f;
        public float orbitSpeed = 15.0f;

        private float currentOrbitAngle = 0f;
        private Vector3 targetPosition;
        private Quaternion targetRotation;

        private void Start()
        {
            FindReferences();
            SetMode(playerTarget != null ? CameraMode.FollowPlayer : CameraMode.Broadcast, immediate: true);
        }

        public void FindReferences()
        {
            if (playerTarget == null)
            {
                PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
                if (pm != null) playerTarget = pm.transform;
            }

            if (arenaCenter == null)
            {
                PokemonArenaBuilder builder = FindFirstObjectByType<PokemonArenaBuilder>();
                if (builder != null) arenaCenter = builder.transform;
            }
        }

        private void Update()
        {
            if (playerTarget == null)
            {
                PlayerMovement pm = FindFirstObjectByType<PlayerMovement>();
                if (pm != null) playerTarget = pm.transform;
            }

            // Keyboard Shortcuts compatible with New Input System
            CheckShortcuts();

            Vector3 center = arenaCenter != null ? arenaCenter.position : Vector3.zero;

            switch (currentMode)
            {
                case CameraMode.FollowPlayer:
                    if (playerTarget != null)
                    {
                        Vector3 playerFwd = GetPlayerFacing(playerTarget);
                        Vector3 behind = playerTarget.position - playerFwd * followDistance + Vector3.up * followHeight;
                        targetPosition = behind;
                        targetRotation = Quaternion.LookRotation((playerTarget.position + Vector3.up * 1.4f) - targetPosition);
                    }
                    else
                    {
                        targetPosition = center + new Vector3(0, 19f, -30f);
                        targetRotation = Quaternion.Euler(32f, 0, 0);
                    }
                    break;

                case CameraMode.Broadcast:
                    targetPosition = center + new Vector3(0, 19f, -30f);
                    targetRotation = Quaternion.Euler(32f, 0, 0);
                    break;

                case CameraMode.Trainer1:
                    targetPosition = center + new Vector3(0, 2.4f, -19.5f);
                    targetRotation = Quaternion.Euler(6f, 0, 0);
                    break;

                case CameraMode.Trainer2:
                    targetPosition = center + new Vector3(0, 2.4f, 19.5f);
                    targetRotation = Quaternion.Euler(6f, 180f, 0);
                    break;

                case CameraMode.BattleCenter:
                    targetPosition = center + new Vector3(-14f, 3.5f, 0);
                    targetRotation = Quaternion.Euler(12f, 90f, 0);
                    break;

                case CameraMode.CinematicOrbit:
                    currentOrbitAngle += orbitSpeed * Time.deltaTime;
                    float rad = currentOrbitAngle * Mathf.Deg2Rad;
                    targetPosition = center + new Vector3(Mathf.Sin(rad) * orbitDistance, orbitHeight, Mathf.Cos(rad) * orbitDistance);
                    targetRotation = Quaternion.LookRotation((center + Vector3.up * 1.5f) - targetPosition);
                    break;

                case CameraMode.FreeLook:
                    HandleFreeLook();
                    return;
            }

            // Smooth Interpolation
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * positionSmoothSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);
        }

        private void CheckShortcuts()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.cKey.wasPressedThisFrame) SetMode(CameraMode.FollowPlayer);
                if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) SetMode(CameraMode.Broadcast);
                if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) SetMode(CameraMode.Trainer1);
                if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) SetMode(CameraMode.Trainer2);
                if (kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame) SetMode(CameraMode.BattleCenter);
                if (kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame) SetMode(CameraMode.CinematicOrbit);
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.C)) SetMode(CameraMode.FollowPlayer);
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetMode(CameraMode.Broadcast);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetMode(CameraMode.Trainer1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetMode(CameraMode.Trainer2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SetMode(CameraMode.BattleCenter);
            if (Input.GetKeyDown(KeyCode.Alpha5)) SetMode(CameraMode.CinematicOrbit);
#endif
        }

        public void SetMode(CameraMode mode, bool immediate = false)
        {
            currentMode = mode;
            Vector3 center = arenaCenter != null ? arenaCenter.position : Vector3.zero;

            switch (mode)
            {
                case CameraMode.FollowPlayer:
                    if (playerTarget != null)
                    {
                        Vector3 playerFwd = GetPlayerFacing(playerTarget);
                        targetPosition = playerTarget.position - playerFwd * followDistance + Vector3.up * followHeight;
                        targetRotation = Quaternion.LookRotation((playerTarget.position + Vector3.up * 1.4f) - targetPosition);
                    }
                    else
                    {
                        targetPosition = center + new Vector3(0, 19f, -30f);
                        targetRotation = Quaternion.Euler(32f, 0, 0);
                    }
                    break;
                case CameraMode.Broadcast:
                    targetPosition = center + new Vector3(0, 19f, -30f);
                    targetRotation = Quaternion.Euler(32f, 0, 0);
                    break;
                case CameraMode.Trainer1:
                    targetPosition = center + new Vector3(0, 2.4f, -19.5f);
                    targetRotation = Quaternion.Euler(6f, 0, 0);
                    break;
                case CameraMode.Trainer2:
                    targetPosition = center + new Vector3(0, 2.4f, 19.5f);
                    targetRotation = Quaternion.Euler(6f, 180f, 0);
                    break;
                case CameraMode.BattleCenter:
                    targetPosition = center + new Vector3(-14f, 3.5f, 0);
                    targetRotation = Quaternion.Euler(12f, 90f, 0);
                    break;
            }

            if (immediate)
            {
                transform.position = targetPosition;
                transform.rotation = targetRotation;
            }
        }

        private Vector3 GetPlayerFacing(Transform target)
        {
            if (target == null) return Vector3.forward;
            return Quaternion.Euler(0f, target.eulerAngles.y, 0f) * Vector3.forward;
        }

        private void HandleFreeLook()
        {
#if ENABLE_INPUT_SYSTEM
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    Vector2 delta = mouse.delta.ReadValue() * 0.15f;
                    transform.Rotate(0, delta.x, 0, Space.World);
                    transform.Rotate(-delta.y, 0, 0, Space.Self);
                }

                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    transform.position += transform.forward * (Mathf.Sign(scroll) * 2f);
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X") * 3f;
                float mouseY = -Input.GetAxis("Mouse Y") * 3f;
                transform.Rotate(0, mouseX, 0, Space.World);
                transform.Rotate(mouseY, 0, 0, Space.Self);
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                transform.position += transform.forward * scroll * 10f;
            }
#endif
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(15, 15, 230, 280), "Arena Camera Controls", GUI.skin.window);
            GUILayout.Space(5);

            if (GUILayout.Button("[C] Follow Character")) SetMode(CameraMode.FollowPlayer);
            if (GUILayout.Button("[1] Broadcast Overview")) SetMode(CameraMode.Broadcast);
            if (GUILayout.Button("[2] Trainer 1 (Red)")) SetMode(CameraMode.Trainer1);
            if (GUILayout.Button("[3] Trainer 2 (Blue)")) SetMode(CameraMode.Trainer2);
            if (GUILayout.Button("[4] Side Duel View")) SetMode(CameraMode.BattleCenter);
            if (GUILayout.Button("[5] Cinematic Orbit")) SetMode(CameraMode.CinematicOrbit);

            GUILayout.Space(8);
            GUILayout.Label($"Active: <b>{currentMode}</b>", GetRichLabelStyle());
            GUILayout.Label("<size=10>Press [C] or [1-5] to switch views</size>", GetRichLabelStyle());

            GUILayout.EndArea();
        }

        private GUIStyle GetRichLabelStyle()
        {
            GUIStyle s = new GUIStyle(GUI.skin.label);
            s.richText = true;
            return s;
        }
    }
}
