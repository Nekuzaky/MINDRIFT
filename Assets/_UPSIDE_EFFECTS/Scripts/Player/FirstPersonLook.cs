using UnityEngine;

namespace UpsideEffects.Player
{
    public sealed class FirstPersonLook : MonoBehaviour
    {
        [Header("Look References")]
        [SerializeField] private Transform yawTransform;
        [SerializeField] private Transform pitchTransform;

        [Header("Sensitivity")]
        [SerializeField] private float mouseSensitivityX = 2f;
        [SerializeField] private float mouseSensitivityY = 2f;
        [SerializeField] private bool invertY;
        [SerializeField] private float smoothing = 18f;

        [Header("Pitch Limits")]
        [SerializeField] private float minPitch = -85f;
        [SerializeField] private float maxPitch = 85f;

        [Header("Cursor")]
        [SerializeField] private bool lockCursorOnStart = true;
        [SerializeField] private KeyCode unlockKey = KeyCode.Escape;

        private float yaw;
        private float pitch;
        private Vector2 smoothedInput;

        private void Awake()
        {
            if (yawTransform == null)
            {
                yawTransform = transform;
            }

            if (pitchTransform == null)
            {
                pitchTransform = transform;
            }

            Vector3 yawEuler = yawTransform.localEulerAngles;
            Vector3 pitchEuler = pitchTransform.localEulerAngles;
            yaw = yawEuler.y;
            pitch = NormalizePitch(pitchEuler.x);
        }

        private void Start()
        {
            if (lockCursorOnStart)
            {
                SetCursorLock(true);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(unlockKey))
            {
                SetCursorLock(false);
            }

            if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
            {
                SetCursorLock(true);
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            float deltaX = Input.GetAxis("Mouse X") * mouseSensitivityX;
            float deltaY = Input.GetAxis("Mouse Y") * mouseSensitivityY * (invertY ? 1f : -1f);

            Vector2 targetInput = new Vector2(deltaX, deltaY);
            smoothedInput = Vector2.Lerp(smoothedInput, targetInput, 1f - Mathf.Exp(-smoothing * Time.deltaTime));

            yaw += smoothedInput.x;
            pitch = Mathf.Clamp(pitch + smoothedInput.y, minPitch, maxPitch);

            yawTransform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            pitchTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        public void SetCursorLock(bool shouldLock)
        {
            Cursor.visible = !shouldLock;
            Cursor.lockState = shouldLock ? CursorLockMode.Locked : CursorLockMode.None;
        }

        private static float NormalizePitch(float value)
        {
            if (value > 180f)
            {
                value -= 360f;
            }

            return value;
        }
    }
}
