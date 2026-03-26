using System;
using UnityEngine;

namespace Mindrift.Core
{
    [Serializable]
    public sealed class PlayerStatsData
    {
        public int totalRuns;
        public int totalDeaths;
        public int topScore;
        public float topHeight;
        public string lastUpdatedUtc;

        public void Sanitize()
        {
            totalRuns = Mathf.Max(0, totalRuns);
            totalDeaths = Mathf.Max(0, totalDeaths);
            topScore = Mathf.Max(0, topScore);
            topHeight = Mathf.Max(0f, topHeight);
            lastUpdatedUtc ??= string.Empty;
        }
    }
}
