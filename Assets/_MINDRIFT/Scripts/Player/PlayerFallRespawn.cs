using System;
using System.Collections;
using UnityEngine;
using Mindrift.Checkpoints;
using Mindrift.Effects;
using Mindrift.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Mindrift.Player
{
    public sealed class PlayerFallRespawn : MonoBehaviour
    {
        [Header("Fall Punishment")]
        [SerializeField] private float killHeight = -20f;
        [SerializeField] private Vector3 respawnPositionOffset = new Vector3(0f, 0.1f, 0f);

        [Header("References")]
        [SerializeField] private FirstPersonMotor firstPersonMotor;
        [SerializeField] private CheckpointManager checkpointManager;
        [SerializeField] private SideEffectUI sideEffectUI;
        [SerializeField] private CameraSideEffects cameraSideEffects;

        [Header("Debug")]
        [SerializeField] private bool logRespawns;

#if ENABLE_INPUT_SYSTEM
        [Header("Controller Haptics")]
        [SerializeField, Range(0f, 1f)] private float deathVibrationLowFrequency = 0.35f;
        [SerializeField, Range(0f, 1f)] private float deathVibrationHighFrequency = 0.75f;
        [SerializeField, Range(0.02f, 1f)] private float deathVibrationDuration = 0.18f;
#endif

        public event Action Respawned;

        public float KillHeight => killHeight;

        private bool isRespawning;
#if ENABLE_INPUT_SYSTEM
        private Coroutine vibrationRoutine;
#endif

        private void Awake()
        {
            if (firstPersonMotor == null)
            {
                firstPersonMotor = GetComponent<FirstPersonMotor>();
            }
        }

        private void Update()
        {
            if (isRespawning)
            {
                return;
            }

            if (transform.position.y < killHeight)
            {
                RespawnAtCheckpoint();
            }
        }

        private void OnDisable()
        {
            StopVibrationIfNeeded();
        }

        private void OnDestroy()
        {
            StopVibrationIfNeeded();
        }

        [ContextMenu("Respawn At Last Checkpoint")]
        public void RespawnAtCheckpoint()
        {
            if (checkpointManager == null)
            {
                checkpointManager = FindFirstObjectByType<CheckpointManager>();
            }

            Transform spawnTransform = checkpointManager != null
                ? checkpointManager.GetCurrentRespawnPoint()
                : null;

            if (spawnTransform == null)
            {
                return;
            }

            isRespawning = true;

            Vector3 targetPosition = spawnTransform.position + respawnPositionOffset;
            Quaternion targetRotation = Quaternion.Euler(0f, spawnTransform.eulerAngles.y, 0f);

            if (firstPersonMotor != null)
            {
                firstPersonMotor.TeleportTo(targetPosition);
            }
            else
            {
                transform.position = targetPosition;
            }

            transform.rotation = targetRotation;

            if (cameraSideEffects != null)
            {
                cameraSideEffects.AddTrauma(0.4f);
            }

            if (sideEffectUI != null)
            {
                sideEffectUI.PlayRespawnFlash();
            }

            TriggerDeathVibration();
            Respawned?.Invoke();

            if (logRespawns)
            {
                Debug.Log($"[MINDRIFT] Respawned at checkpoint {spawnTransform.name}.");
            }

            isRespawning = false;
        }

        public void SetCheckpointManager(CheckpointManager manager)
        {
            checkpointManager = manager;
        }

        private void TriggerDeathVibration()
        {
#if ENABLE_INPUT_SYSTEM
            if (!SettingsManager.ControllerVibrationEnabled)
            {
                return;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return;
            }

            if (vibrationRoutine != null)
            {
                StopCoroutine(vibrationRoutine);
            }

            vibrationRoutine = StartCoroutine(DeathVibrationPulseRoutine(gamepad));
#endif
        }

        private void StopVibrationIfNeeded()
        {
#if ENABLE_INPUT_SYSTEM
            if (vibrationRoutine != null)
            {
                StopCoroutine(vibrationRoutine);
                vibrationRoutine = null;
            }

            if (Gamepad.current != null)
            {
                Gamepad.current.ResetHaptics();
            }
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private IEnumerator DeathVibrationPulseRoutine(Gamepad gamepad)
        {
            if (gamepad == null)
            {
                vibrationRoutine = null;
                yield break;
            }

            float low = Mathf.Clamp01(deathVibrationLowFrequency);
            float high = Mathf.Clamp01(deathVibrationHighFrequency);
            float duration = Mathf.Clamp(deathVibrationDuration, 0.02f, 1f);

            gamepad.SetMotorSpeeds(low, high);

            float endTime = Time.unscaledTime + duration;
            while (Time.unscaledTime < endTime)
            {
                if (!SettingsManager.ControllerVibrationEnabled || gamepad != Gamepad.current)
                {
                    break;
                }

                yield return null;
            }

            gamepad.ResetHaptics();
            vibrationRoutine = null;
        }
#endif
    }
}
