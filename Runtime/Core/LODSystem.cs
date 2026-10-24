using System;
using System.Collections.Generic;
using System.Numerics;

namespace LODSim.Core
{
    public sealed class LODSystem
    {
        public event Action<ILODSubject, LODLevel, LODLevel> LevelChanged;

        private readonly LODProfile _profile;
        private readonly Random _random;

        private readonly List<LODEntry> _entries = new();
        private readonly List<LODEntry> _pending = new();
        private readonly List<LODEntry> _applyBuffer = new();
        private readonly List<ILODViewer> _viewers = new();

        private Vector3[] _viewerPositions = new Vector3[4];

        private int _viewerPositionCount;
        private double _time;
        private double _nextEvaluationTime;
        private bool _needsCompaction;

        public LODProfile Profile => _profile;
        public Action<Exception, ILODSubject> ExceptionHandler { get; set; }

        public LODSystem(LODProfile profile, int randomSeed = 0)
        {
            ThrowIfNull(profile, nameof(profile));

            _profile = profile;
            _random = new(randomSeed);
        }

        public void AddViewer(ILODViewer viewer)
        {
            ThrowIfNull(viewer, nameof(viewer));

            if (!_viewers.Contains(viewer))
                _viewers.Add(viewer);
        }

        public bool RemoveViewer(ILODViewer viewer)
        {
            ThrowIfNull(viewer, nameof(viewer));

            return _viewers.Remove(viewer);
        }

        public LODHandle Register(ILODSubject subject)
        {
            ThrowIfNull(subject, nameof(subject));

            LODEntry entry = new(subject);
            _pending.Add(entry);

            return new(this, entry);
        }

        public void Update(float dt)
        {
            if (dt > 0f)
                _time += dt;

            bool hasViewers = _viewers.Count > 0;
            bool evaluateAll = hasViewers && _time >= _nextEvaluationTime;

            if (hasViewers && (evaluateAll || _pending.Count > 0))
                SnapshotViewerPositions();

            ApplyPending(hasViewers && !evaluateAll);

            if (evaluateAll)
            {
                _nextEvaluationTime = _time + _profile.EvaluationInterval;
                EvaluateAll();
            }

            Tick();
            Compact();
        }

        internal void Unregister(LODEntry entry)
        {
            if (entry.Removed)
                return;

            entry.Removed = true;
            _needsCompaction = true;
        }

        private void ApplyPending(bool evaluateNew)
        {
            if (_pending.Count == 0)
                return;

            _applyBuffer.Clear();
            _applyBuffer.AddRange(_pending);
            _pending.Clear();

            for (int i = 0; i < _applyBuffer.Count; i++)
            {
                LODEntry entry = _applyBuffer[i];

                if (entry.Removed)
                    continue;

                _entries.Add(entry);

                if (evaluateNew)
                    EvaluateEntry(entry);
            }

            _applyBuffer.Clear();
        }

        private void EvaluateAll()
        {
            for (int i = 0; i < _entries.Count; i++)
                EvaluateEntry(_entries[i]);
        }

        private void EvaluateEntry(LODEntry entry)
        {
            if (entry.Removed || entry.Faulted || _viewerPositionCount == 0)
                return;

            if (!TryGetPosition(entry, out Vector3 position))
                return;

            LODLevel target = _profile.Evaluate(GetMinSqrDistance(position), entry.Level);

            if (target != entry.Level)
                TryTransition(entry, target);
        }

        private void TryTransition(LODEntry entry, LODLevel to)
        {
            LODLevel from = entry.Level;

            if (!TryApplyTransition(entry, from, to))
                return;

            float interval = _profile.GetTickInterval(to);

            entry.Level = to;
            entry.LastTickTime = _time;
            entry.NextTickTime = _time + interval * (0.5 + _random.NextDouble());

            try
            {
                LevelChanged?.Invoke(entry.Subject, from, to);
            }
            catch (Exception exception)
            {
                ExceptionHandler?.Invoke(exception, entry.Subject);
            }
        }

        private void Tick()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                LODEntry entry = _entries[i];

                if (entry.Removed || entry.Faulted)
                    continue;

                float interval = _profile.GetTickInterval(entry.Level);

                if (interval <= 0f || _time < entry.NextTickTime)
                    continue;

                float delta = (float)(_time - entry.LastTickTime);
                entry.LastTickTime = _time;
                entry.NextTickTime = _time + interval;

                TickEntry(entry, delta);
            }
        }

        private void Compact()
        {
            if (!_needsCompaction)
                return;

            int write = 0;

            for (int read = 0; read < _entries.Count; read++)
            {
                LODEntry entry = _entries[read];

                if (!entry.Removed)
                    _entries[write++] = entry;
            }

            _entries.RemoveRange(write, _entries.Count - write);
            _needsCompaction = false;
        }

        private bool TryGetPosition(LODEntry entry, out Vector3 position)
        {
            try
            {
                position = entry.Subject.Position;
                return true;
            }
            catch (Exception exception)
            {
                position = default;
                Fault(entry, exception);
                return false;
            }
        }

        private bool TryApplyTransition(LODEntry entry, LODLevel from, LODLevel to)
        {
            try
            {
                if (!entry.Subject.CanChangeLOD(to))
                    return false;

                entry.Subject.OnLODChanged(from, to);
                return true;
            }
            catch (Exception exception)
            {
                Fault(entry, exception);
                return false;
            }
        }

        private void TickEntry(LODEntry entry, float delta)
        {
            try
            {
                entry.Subject.OnLODTick(entry.Level, delta);
            }
            catch (Exception exception)
            {
                Fault(entry, exception);
            }
        }

        private void Fault(LODEntry entry, Exception exception)
        {
            entry.Faulted = true;
            ExceptionHandler?.Invoke(exception, entry.Subject);
        }

        private void SnapshotViewerPositions()
        {
            int count = _viewers.Count;

            if (_viewerPositions.Length < count)
                Array.Resize(ref _viewerPositions, count);

            int written = 0;

            for (int i = 0; i < count; i++)
            {
                try
                {
                    _viewerPositions[written] = _viewers[i].Position;
                    written++;
                }
                catch (Exception exception)
                {
                    ExceptionHandler?.Invoke(exception, null);
                }
            }

            _viewerPositionCount = written;
        }

        private float GetMinSqrDistance(Vector3 position)
        {
            float min = float.PositiveInfinity;

            for (int i = 0; i < _viewerPositionCount; i++)
            {
                float sqr = Vector3.DistanceSquared(position, _viewerPositions[i]);

                if (sqr < min)
                    min = sqr;
            }

            return min;
        }

        private static void ThrowIfNull(object value, string paramName)
        {
            if (value == null)
                throw new ArgumentNullException(paramName);
        }
    }
}