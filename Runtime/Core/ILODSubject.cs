using System.Numerics;

namespace LODSim.Core
{
    public interface ILODSubject
    {
        public Vector3 Position { get; }
        public bool CanChangeLOD(LODLevel to);
        public void OnLODChanged(LODLevel from, LODLevel to);
        public void OnLODTick(LODLevel level, float dt);
    }
}
