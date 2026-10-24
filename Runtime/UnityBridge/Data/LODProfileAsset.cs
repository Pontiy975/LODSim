using System;
using UnityEngine;
using LODSim.Core;

namespace LODSim.UnityBridge
{
    [CreateAssetMenu(menuName = "LODSim/Profile", fileName = "LODProfile")]
    public sealed class LODProfileAsset : ScriptableObject
    {
        [SerializeField, Min(0.01f)] private float _onlineDistance = 150f;
        [SerializeField, Min(0f)] private float _hysteresis = 20f;
        [SerializeField, Min(0f)] private float _offlineTickInterval = 0.5f;
        [SerializeField, Min(0.01f)] private float _evaluationInterval = 0.25f;

        public LODProfile ToProfile() =>
            new(_onlineDistance, _hysteresis, _offlineTickInterval, _evaluationInterval);

        private void OnValidate()
        {
            try
            {
                ToProfile();
            }
            catch (ArgumentException exception)
            {
                Debug.LogError(exception.Message, this);
            }
        }
    }
}