using Mindrift.Player;
using UnityEngine;

namespace Mindrift.Core
{
    public sealed class PlayerStatsTracker : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RunSessionManager runSessionManager;
        [SerializeField] private PlayerFallRespawn playerFallRespawn;
        [SerializeField] private HeightProgressionManager heightProgressionManager;

        [Header("Persistence")]
        [SerializeField] private float saveIntervalSeconds = 0.75f;

        [Header("Debug")]
        [SerializeField] private bool logStatChanges;

        private PlayerStatsData stats;
        private float currentRunPeakHeight;
        private float nextSaveTime;
        private bool dirty;

        private void Awake()
        {
            ResolveReferences();
            stats = PlayerStatsStorage.Load();
            currentRunPeakHeight = GetObservedHeight();
            nextSaveTime = Time.unscaledTime + Mathf.Max(0.1f, saveIntervalSeconds);
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe(true);
        }

        private void Start()
        {
            ObserveProgress();
            FlushIfDirty(force: true);
        }

        private void Update()
        {
            ObserveProgress();
            FlushIfDirty(force: false);
        }

        private void OnDisable()
        {
            Subscribe(false);
            FlushIfDirty(force: true);
        }

        private void OnApplicationQuit()
        {
            FlushIfDirty(force: true);
        }

        private void ResolveReferences()
        {
            if (runSessionManager == null)
            {
                runSessionManager = FindFirstObjectByType<RunSessionManager>();
            }

            if (playerFallRespawn == null)
            {
                playerFallRespawn = FindFirstObjectByType<PlayerFallRespawn>();
            }

            if (heightProgressionManager == null)
            {
                heightProgressionManager = FindFirstObjectByType<HeightProgressionManager>();
            }
        }

        private void Subscribe(bool subscribe)
        {
            if (runSessionManager != null)
            {
                if (subscribe)
                {
                    runSessionManager.RunStarted += HandleRunStarted;
                    runSessionManager.RunCompleted += HandleRunCompleted;
                }
                else
                {
                    runSessionManager.RunStarted -= HandleRunStarted;
                    runSessionManager.RunCompleted -= HandleRunCompleted;
                }
            }

            if (playerFallRespawn != null)
            {
                if (subscribe)
                {
                    playerFallRespawn.Respawned += HandleRespawned;
                }
                else
                {
                    playerFallRespawn.Respawned -= HandleRespawned;
                }
            }
        }

        private void HandleRunStarted()
        {
            stats.totalRuns++;
            currentRunPeakHeight = GetObservedHeight();
            UpdateTopHeight(currentRunPeakHeight);
            UpdateTopScore(completed: false);
            MarkDirty($"Run started. TotalRuns={stats.totalRuns}");
        }

        private void HandleRunCompleted(RunSessionManager.RunResult _)
        {
            ObserveProgress();
            UpdateTopScore(completed: true);
            MarkDirty($"Run completed. TopScore={stats.topScore}");
        }

        private void HandleRespawned()
        {
            stats.totalDeaths++;
            UpdateTopScore(completed: false);
            MarkDirty($"Respawn counted. TotalDeaths={stats.totalDeaths}");
        }

        private void ObserveProgress()
        {
            float observedHeight = GetObservedHeight();
            if (runSessionManager != null && runSessionManager.IsRunning)
            {
                currentRunPeakHeight = Mathf.Max(currentRunPeakHeight, observedHeight);
            }
            else
            {
                currentRunPeakHeight = Mathf.Max(currentRunPeakHeight, observedHeight);
            }

            UpdateTopHeight(currentRunPeakHeight);
            UpdateTopScore(completed: runSessionManager != null && runSessionManager.IsCompleted);
        }

        private float GetObservedHeight()
        {
            if (heightProgressionManager != null)
            {
                return heightProgressionManager.CurrentHeight;
            }

            if (playerFallRespawn != null)
            {
                return playerFallRespawn.transform.position.y;
            }

            return 0f;
        }

        private void UpdateTopHeight(float candidateHeight)
        {
            float safeHeight = Mathf.Max(0f, candidateHeight);
            if (safeHeight <= stats.topHeight)
            {
                return;
            }

            stats.topHeight = safeHeight;
            MarkDirty($"TopHeight={stats.topHeight:0.00}");
        }

        private void UpdateTopScore(bool completed)
        {
            int candidateScore = CalculateCurrentScore(completed);
            if (candidateScore <= stats.topScore)
            {
                return;
            }

            stats.topScore = candidateScore;
            MarkDirty($"TopScore={stats.topScore}");
        }

        // Temporary local score formula so the profile has a meaningful metric
        // before a dedicated gameplay scoring system exists.
        private int CalculateCurrentScore(bool completed)
        {
            int checkpoints = runSessionManager != null ? runSessionManager.CheckpointCount : 0;
            int falls = runSessionManager != null ? runSessionManager.FallCount : 0;
            int baseScore = Mathf.RoundToInt(Mathf.Max(0f, currentRunPeakHeight) * 10f);
            int checkpointBonus = checkpoints * 250;
            int fallPenalty = falls * 50;
            int completionBonus = completed ? 1000 : 0;
            return Mathf.Max(0, baseScore + checkpointBonus + completionBonus - fallPenalty);
        }

        private void MarkDirty(string message)
        {
            dirty = true;
            if (!logStatChanges)
            {
                return;
            }

            Debug.Log($"[MINDRIFT] Player stats updated. {message}");
        }

        private void FlushIfDirty(bool force)
        {
            if (!dirty)
            {
                return;
            }

            float interval = Mathf.Max(0.1f, saveIntervalSeconds);
            if (!force && Time.unscaledTime < nextSaveTime)
            {
                return;
            }

            PlayerStatsStorage.Save();
            dirty = false;
            nextSaveTime = Time.unscaledTime + interval;
        }
    }
}
