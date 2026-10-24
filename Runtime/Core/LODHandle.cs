using System;

namespace LODSim.Core
{
    public sealed class LODHandle : IDisposable
    {
        private readonly LODSystem _system;
        private readonly LODEntry _entry;

        public ILODSubject Subject => _entry.Subject;
        public LODLevel Level => _entry.Level;
        public bool IsValid => !_entry.Removed;

        internal LODHandle(LODSystem system, LODEntry entry)
        {
            _system = system;
            _entry = entry;
        }

        public void Dispose() => _system.Unregister(_entry);
    }

    internal sealed class LODEntry
    {
        internal ILODSubject Subject { get; }
        internal LODLevel Level { get; set; }
        internal double LastTickTime { get; set; }
        internal double NextTickTime { get; set; }
        internal bool Removed { get; set; }
        internal bool Faulted { get; set; }

        internal LODEntry(ILODSubject subject)
        {
            Subject = subject;
            Level = LODLevel.Online;
        }
    }
}