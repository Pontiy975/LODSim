namespace LODSim.UnityBridge
{
    public static class LODVectorExtensions
    {
        public static System.Numerics.Vector3 ToLOD(this UnityEngine.Vector3 value) => new(value.x, value.y, value.z);
        public static UnityEngine.Vector3 ToUnity(this System.Numerics.Vector3 value) => new(value.X, value.Y, value.Z);
    }
}