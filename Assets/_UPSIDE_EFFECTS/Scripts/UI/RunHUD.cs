using UnityEngine;
using UnityEngine.UI;
using UpsideEffects.Core;

namespace UpsideEffects.UI
{
    public sealed class RunHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RunSessionManager runSessionManager;
        [SerializeField] private Component timerTextComponent;
        [SerializeField] private Component summaryTextComponent;
        [SerializeField] private CanvasGroup summaryCanvasGroup;

        [Header("Labels")]
        [SerializeField] private string timerPrefix = "RUN";
        [SerializeField] private string completedPrefix = "RUN COMPLETE";

        private void Awake()
        {
            if (runSessionManager == null)
            {
                runSessionManager = FindFirstObjectByType<RunSessionManager>();
            }

            if (summaryCanvasGroup != null)
            {
                summaryCanvasGroup.alpha = 0f;
            }
        }

        private void OnEnable()
        {
            if (runSessionManager == null)
            {
                return;
            }

            runSessionManager.TimeUpdated += HandleTimeUpdated;
            runSessionManager.RunStarted += HandleRunStarted;
            runSessionManager.RunCompleted += HandleRunCompleted;
        }

        private void OnDisable()
        {
            if (runSessionManager == null)
            {
                return;
            }

            runSessionManager.TimeUpdated -= HandleTimeUpdated;
            runSessionManager.RunStarted -= HandleRunStarted;
            runSessionManager.RunCompleted -= HandleRunCompleted;
        }

        private void HandleRunStarted()
        {
            if (summaryCanvasGroup != null)
            {
                summaryCanvasGroup.alpha = 0f;
            }

            if (summaryTextComponent != null)
            {
                UITextUtility.SetText(summaryTextComponent, string.Empty);
            }
        }

        private void HandleTimeUpdated(float elapsed)
        {
            if (timerTextComponent == null)
            {
                return;
            }

            UITextUtility.SetText(timerTextComponent, $"{timerPrefix}: {RunSessionManager.FormatTime(elapsed)}");
        }

        private void HandleRunCompleted(RunSessionManager.RunResult result)
        {
            if (summaryTextComponent != null)
            {
                string summary = $"{completedPrefix}\n" +
                                 $"TIME: {RunSessionManager.FormatTime(result.TimeSeconds)}\n" +
                                 $"FALLS: {result.Falls}\n" +
                                 $"CHECKPOINTS: {result.Checkpoints}\n" +
                                 "PRESS R TO RESTART";
                UITextUtility.SetText(summaryTextComponent, summary);
            }

            if (summaryCanvasGroup != null)
            {
                summaryCanvasGroup.alpha = 1f;
            }
        }
    }
}
