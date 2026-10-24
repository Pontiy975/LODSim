using LODSim.Core;
using UnityEngine;

namespace LODSim.UnityBridge
{
    public sealed class LODViewer : MonoBehaviour, ILODViewer
    {
        private LODSystem _system;
        private bool _added;

        public System.Numerics.Vector3 Position => transform.position.ToLOD();

        public void Initialize(LODSystem system)
        {
            if (system == null)
                throw new System.ArgumentNullException(nameof(system));

            if (ReferenceEquals(_system, system))
                return;

            Remove();
            _system = system;

            if (isActiveAndEnabled)
                Add();
        }

        private void OnEnable() => Add();

        private void OnDisable() => Remove();

        private void OnDestroy() => Remove();

        private void Add()
        {
            if (_added || _system == null)
                return;

            _system.AddViewer(this);
            _added = true;
        }

        private void Remove()
        {
            if (!_added)
                return;

            _system.RemoveViewer(this);
            _added = false;
        }
    }
}