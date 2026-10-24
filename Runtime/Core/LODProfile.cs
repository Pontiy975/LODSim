using System;

namespace LODSim.Core
{
    public sealed class LODProfile
    {
        private readonly float _leaveOnlineSqr;
        private readonly float _enterOnlineSqr;

        public float OnlineDistance { get; }
        public float Hysteresis { get; }
        public float OfflineTickInterval { get; }
        public float EvaluationInterval { get; }

        public LODProfile(float onlineDistance, float hysteresis, float offlineTickInterval, float evaluationInterval)
        {
            if (!float.IsFinite(onlineDistance) || onlineDistance <= 0f)
                ThrowArgumentException(nameof(onlineDistance), "OnlineDistance must be a finite positive number.");

            if (!float.IsFinite(hysteresis) || hysteresis < 0f)
                ThrowArgumentException(nameof(hysteresis), "Hysteresis must be a finite non-negative number.");

            if (!float.IsFinite(offlineTickInterval) || offlineTickInterval < 0f)
                ThrowArgumentException(nameof(offlineTickInterval), "OfflineTickInterval must be a finite non-negative number.");

            if (!float.IsFinite(evaluationInterval) || evaluationInterval <= 0f)
                ThrowArgumentException(nameof(evaluationInterval), "EvaluationInterval must be a finite positive number.");

            OnlineDistance = onlineDistance;
            Hysteresis = hysteresis;
            OfflineTickInterval = offlineTickInterval;
            EvaluationInterval = evaluationInterval;

            float leave = onlineDistance + hysteresis;
            _leaveOnlineSqr = leave * leave;
            _enterOnlineSqr = onlineDistance * onlineDistance;
        }

        public float GetTickInterval(LODLevel level) =>
            level == LODLevel.Offline ? OfflineTickInterval : 0f;

        public LODLevel Evaluate(float sqrDistance, LODLevel current) => current == LODLevel.Online
                ? sqrDistance > _leaveOnlineSqr ? LODLevel.Offline : LODLevel.Online
                : sqrDistance <= _enterOnlineSqr ? LODLevel.Online : LODLevel.Offline;

        private static void ThrowArgumentException(string paramName, string message) =>
            throw new ArgumentException(message, paramName);
    }
}