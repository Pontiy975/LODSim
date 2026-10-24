using LODSim.Core;
using System;
using UnityEngine;

namespace LODSim.UnityBridge
{
    [DefaultExecutionOrder(-100)]
    public sealed class LODRunner : MonoBehaviour
    {
        [SerializeField] private LODProfileAsset _profileAsset;
        [SerializeField, Min(0.01f)] private float _maxFrameDelta = 0.1f;

        private LODSystem _system;

        public LODSystem System
        {
            get
            {
                _system ??= CreateSystem();
                return _system;
            }
        }

        private void Awake()
        {
            try
            {
                _ = System;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                enabled = false;
            }
        }

        private void Update()
        {
            _system.Update(Mathf.Min(Time.deltaTime, _maxFrameDelta));
        }

        private LODSystem CreateSystem()
        {
            if (_profileAsset == null)
                throw new InvalidOperationException("LODRunner has no profile asset assigned.");

            LODSystem system = new(_profileAsset.ToProfile());
            system.ExceptionHandler = HandleSubjectException;

            return system;
        }

        private static void HandleSubjectException(Exception exception, ILODSubject subject) =>
            Debug.LogException(exception, subject as UnityEngine.Object);
    }
}