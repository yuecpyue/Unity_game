using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PokemonArena
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Speeds")]
        public float walkSpeed = 5.0f;
        public float runSpeed = 8.5f;
        public float rotationSpeed = 720.0f;
        public float jumpHeight = 1.2f;
        public float gravity = 20.0f;

        [Header("Camera Reference")]
        public Transform cameraTransform;

        [Header("Model Orientation Offset")]
        [Tooltip("Orientation offset for models imported with axis differences (e.g. -90 on X for Blender/FBX models)")]
        public Vector3 rotationOffset = new Vector3(-90f, 0f, 0f);

        [Header("Animation (Optional)")]
        public Animator animator;

        private CharacterController controller;
        private Vector3 verticalVelocity = Vector3.zero;
        private bool isGrounded;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        private void Start()
        {
            if (rotationOffset != Vector3.zero)
            {
                transform.rotation = Quaternion.Euler(rotationOffset);
            }
        }

        private void Update()
        {
            isGrounded = controller.isGrounded;
            if (isGrounded && verticalVelocity.y < 0)
            {
                verticalVelocity.y = -2f; // Downward stick force
            }

            // 1. Read Input (Compatible with New Input System & Legacy)
            Vector2 moveInput = ReadMoveInput();
            Vector3 inputDir = new Vector3(moveInput.x, 0f, moveInput.y);
            if (inputDir.sqrMagnitude > 1f) inputDir.Normalize();

            // 2. Sprinting (Shift)
            bool isSprinting = ReadSprintInput();
            float currentSpeed = (isSprinting && inputDir.magnitude > 0.1f) ? runSpeed : walkSpeed;

            // 3. Direction relative to Camera or World
            Vector3 moveDirection = Vector3.zero;
            if (inputDir.magnitude > 0.05f)
            {
                if (cameraTransform != null)
                {
                    Vector3 camFwd = cameraTransform.forward;
                    camFwd.y = 0f;
                    camFwd.Normalize();

                    Vector3 camRight = cameraTransform.right;
                    camRight.y = 0f;
                    camRight.Normalize();

                    moveDirection = (camFwd * inputDir.z + camRight * inputDir.x).normalized;
                }
                else
                {
                    moveDirection = inputDir;
                }

                // Smooth Rotation towards movement direction while maintaining model pitch offset
                Quaternion yawRot = Quaternion.LookRotation(moveDirection, Vector3.up);
                Quaternion targetRot = yawRot * Quaternion.Euler(rotationOffset);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            // 4. Horizontal Movement
            Vector3 motion = moveDirection * currentSpeed;

            // 5. Jump & Gravity
            if (ReadJumpInput() && isGrounded)
            {
                verticalVelocity.y = Mathf.Sqrt(jumpHeight * 2f * gravity);
            }

            verticalVelocity.y -= gravity * Time.deltaTime;
            motion += verticalVelocity;

            // 6. Apply Movement
            controller.Move(motion * Time.deltaTime);

            // 7. Update Animator if available
            UpdateAnimator(inputDir.magnitude * (isSprinting ? 2f : 1f));
        }

        private Vector2 ReadMoveInput()
        {
            float moveX = 0f;
            float moveZ = 0f;

#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) moveZ += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) moveZ -= 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) moveX -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) moveX += 1f;
            }

            var gp = Gamepad.current;
            if (gp != null)
            {
                Vector2 stick = gp.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.04f)
                {
                    moveX += stick.x;
                    moveZ += stick.y;
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            moveX = Input.GetAxisRaw("Horizontal");
            moveZ = Input.GetAxisRaw("Vertical");
#endif

            return new Vector2(moveX, moveZ);
        }

        private bool ReadSprintInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)) return true;

            var gp = Gamepad.current;
            if (gp != null && (gp.leftStickButton.isPressed || gp.rightTrigger.isPressed)) return true;

            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#else
            return false;
#endif
        }

        private bool ReadJumpInput()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame) return true;

            var gp = Gamepad.current;
            if (gp != null && gp.buttonSouth.wasPressedThisFrame) return true;

            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space);
#else
            return false;
#endif
        }

        private void UpdateAnimator(float speedValue)
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.SetFloat("Speed", speedValue);
            foreach (var param in animator.parameters)
            {
                if (param.name == "Speed" && param.type == AnimatorControllerParameterType.Float)
                {
                    animator.SetFloat("Speed", speedValue);
                }
                else if (param.name == "IsMoving" && param.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool("IsMoving", speedValue > 0.1f);
                }
                else if (param.name == "MoveSpeed" && param.type == AnimatorControllerParameterType.Float)
                {
                    animator.SetFloat("MoveSpeed", speedValue);
                }
                else if (param.name == "IsGrounded" && param.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool("IsGrounded", isGrounded);
                }
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(15, Screen.height - 95, 260, 80), GUI.skin.box);
            GUILayout.Label("<b>Player Controls</b>");
            GUILayout.Label("• <b>WASD / Arrows</b>: Move Character");
            GUILayout.Label("• <b>Shift</b>: Sprint | <b>Space</b>: Jump");
            GUILayout.EndArea();
        }
    }
}
