using System;
using System.Collections.Generic;
using LODSim.Core;
using UnityEngine;

namespace LODSim.UnityBridge
{
    public sealed class LODAgent : MonoBehaviour, ILODSubject
    {
        private LODSystem _system;
        private LODHandle _handle;

        private ILODSimulated _simulated;
        private ILODAware[] _aware = Array.Empty<ILODAware>();

        public LODLevel CurrentLevel { get; private set; } = LODLevel.Online;

        System.Numerics.Vector3 ILODSubject.Position => _simulated.Position.ToLOD();

        public void Initialize(LODSystem system)
        {
            if (system == null)
                throw new ArgumentNullException(nameof(system));

            if (ReferenceEquals(_system, system))
                return;

            Unregister();
            _system = system;

            if (isActiveAndEnabled)
                TryRegister();
        }

        private void Awake()
        {
            ILODSimulated[] owners = GetComponentsInChildren<ILODSimulated>(true);

            if (owners.Length != 1)
            {
                Debug.LogError($"{nameof(LODAgent)} requires exactly one {nameof(ILODSimulated)} on the unit, found {owners.Length}.", this);
                enabled = false;
                return;
            }

            _simulated = owners[0];

            ILODAware[] all = GetComponentsInChildren<ILODAware>(true);
            List<ILODAware> others = new(all.Length);

            for (int i = 0; i < all.Length; i++)
            {
                if (!ReferenceEquals(all[i], _simulated))
                    others.Add(all[i]);
            }

            _aware = others.ToArray();
        }

        private void OnEnable() => TryRegister();

        private void OnDisable() => Unregister();

        private void OnDestroy() => Unregister();

        public bool CanChangeLOD(LODLevel to) => _simulated.CanChangeLOD(to);

        public void OnLODTick(LODLevel level, float dt) => _simulated.OnLODTick(level, dt);

        public void OnLODChanged(LODLevel from, LODLevel to)
        {
            if (to > from)
            {
                _simulated.OnLODChanged(from, to);
                NotifyAware(from, to);
            }
            else
            {
                NotifyAware(from, to);
                _simulated.OnLODChanged(from, to);
            }

            CurrentLevel = to;
        }

        private void NotifyAware(LODLevel from, LODLevel to)
        {
            for (int i = 0; i < _aware.Length; i++)
                _aware[i].OnLODChanged(from, to);
        }

        private void TryRegister()
        {
            if (_system == null || _simulated == null || _handle != null)
                return;

            if (CurrentLevel != LODLevel.Online)
            {
                try
                {
                    OnLODChanged(CurrentLevel, LODLevel.Online);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                    return;
                }
            }

            _handle = _system.Register(this);
        }

        private void Unregister()
        {
            if (_handle == null)
                return;

            _handle.Dispose();
            _handle = null;
        }
    }
}