using UnityEngine;
using UpsideEffects.World;

namespace UpsideEffects.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FirstPersonMotor : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private Transform movementReference;
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float acceleration = 30f;
        [SerializeField, Range(0f, 1f)] private float airControl = 0.4f;

        [Header("Jump + Gravity")]
        [SerializeField] private float jumpVelocity = 11f;
        [SerializeField] private float gravity = -22f;
        [SerializeField] private float groundedSnapVelocity = -4f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.12f;

        [Header("Debug")]
        [SerializeField] private bool showDebug;

        private CharacterController characterController;
        private Vector3 planarVelocity;
        private float verticalVelocity;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private bool wasGrounded;

        private MovingPlatform currentGroundPlatform;

        public bool IsGrounded => characterController != null && characterController.isGrounded;
        public Vector3 Velocity => planarVelocity + Vector3.up * verticalVelocity;
        public CharacterController CharacterController => characterController;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (movementReference == null)
            {
                movementReference = transform;
            }
        }

        private void Update()
        {
            if (characterController == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            UpdateTimers(deltaTime);
            ApplyMovingPlatformDelta();
            HandleMovement(deltaTime);
            HandleJumpAndGravity(deltaTime);
            CommitMovement(deltaTime);
            currentGroundPlatform = null;
        }

        public void ResetVelocity()
        {
            planarVelocity = Vector3.zero;
            verticalVelocity = 0f;
            coyoteTimer = 0f;
            jumpBufferTimer = 0f;
        }

        public void TeleportTo(Vector3 worldPosition)
        {
            if (characterController == null)
            {
                transform.position = worldPosition;
                return;
            }

            bool wasEnabled = characterController.enabled;
            characterController.enabled = false;
            transform.position = worldPosition;
            characterController.enabled = wasEnabled;
            ResetVelocity();
        }

        private void UpdateTimers(float deltaTime)
        {
            if (Input.GetButtonDown("Jump"))
            {
                jumpBufferTimer = jumpBufferTime;
            }
            else
            {
                jumpBufferTimer -= deltaTime;
            }

            if (characterController.isGrounded)
            {
                coyoteTimer = coyoteTime;
            }
            else
            {
                coyoteTimer -= deltaTime;
            }
        }

        private void HandleMovement(float deltaTime)
        {
            float inputX = Input.GetAxisRaw("Horizontal");
            float inputZ = Input.GetAxisRaw("Vertical");

            Vector3 rawInput = Vector3.ClampMagnitude(new Vector3(inputX, 0f, inputZ), 1f);
            Vector3 referenceForward = movementReference.forward;
            Vector3 referenceRight = movementReference.right;
            referenceForward.y = 0f;
            referenceRight.y = 0f;
            referenceForward.Normalize();
            referenceRight.Normalize();

            Vector3 desiredPlanarVelocity = (referenceForward * rawInput.z + referenceRight * rawInput.x) * moveSpeed;
            float currentAcceleration = characterController.isGrounded ? acceleration : acceleration * airControl;
            planarVelocity = Vector3.MoveTowards(planarVelocity, desiredPlanarVelocity, currentAcceleration * deltaTime);
        }

        private void HandleJumpAndGravity(float deltaTime)
        {
            bool grounded = characterController.isGrounded;

            if (grounded && verticalVelocity < groundedSnapVelocity)
            {
                verticalVelocity = groundedSnapVelocity;
            }

            bool canJump = coyoteTimer > 0f && jumpBufferTimer > 0f;
            if (canJump)
            {
                verticalVelocity = jumpVelocity;
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
            }

            verticalVelocity += gravity * deltaTime;
        }

        private void CommitMovement(float deltaTime)
        {
            Vector3 frameMotion = (planarVelocity + Vector3.up * verticalVelocity) * deltaTime;
            CollisionFlags flags = characterController.Move(frameMotion);

            bool groundedNow = (flags & CollisionFlags.Below) != 0 || characterController.isGrounded;
            if (groundedNow && !wasGrounded && showDebug)
            {
                Debug.Log("[UPSIDE_EFFECTS] Player grounded.");
            }

            wasGrounded = groundedNow;
        }

        private void ApplyMovingPlatformDelta()
        {
            if (currentGroundPlatform == null || !characterController.isGrounded)
            {
                return;
            }

            Vector3 delta = currentGroundPlatform.FrameDelta;
            if (delta.sqrMagnitude > 0f)
            {
                characterController.Move(delta);
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y < 0.55f)
            {
                return;
            }

            MovingPlatform platform = hit.collider.GetComponentInParent<MovingPlatform>();
            if (platform != null)
            {
                currentGroundPlatform = platform;
            }
        }
    }
}
