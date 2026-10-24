using LODSim.Core;
using UnityEngine;

namespace LODSim.UnityBridge
{
    public interface ILODAware
    {
        public void OnLODChanged(LODLevel from, LODLevel to);
    }

    public interface ILODSimulated : ILODAware
    {
        public Vector3 Position { get; }
        public bool CanChangeLOD(LODLevel to);
        public void OnLODTick(LODLevel level, float dt);
    }
}
