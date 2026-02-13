using System;
using System.Collections.Generic;

namespace EVEMon.Common.QueryMonitor
{
    /// <summary>
    /// Centralized scheduler that drives all CharacterDataQuerying and CorporationDataQuerying
    /// instances from a single FiveSecondTick subscription.
    ///
    /// Before this class, each character's querying objects subscribed individually to FiveSecondTick,
    /// producing 2N handlers for N characters. This class reduces that to 1 handler total.
    /// </summary>
    internal sealed class CentralQueryScheduler : IDisposable
    {
        private readonly List<CharacterDataQuerying> _characterQuerying = new List<CharacterDataQuerying>();
        private readonly List<CorporationDataQuerying> _corporationQuerying = new List<CorporationDataQuerying>();
        private readonly object _lock = new object();
        private bool _disposed;

        /// <summary>
        /// Initializes the scheduler and subscribes to FiveSecondTick.
        /// </summary>
        public CentralQueryScheduler()
        {
            EveMonClient.FiveSecondTick += OnFiveSecondTick;
        }

        /// <summary>
        /// Registers a CharacterDataQuerying instance to be driven by this scheduler.
        /// </summary>
        public void Register(CharacterDataQuerying querying)
        {
            if (querying == null || _disposed)
                return;

            lock (_lock)
            {
                if (!_characterQuerying.Contains(querying))
                    _characterQuerying.Add(querying);
            }
        }

        /// <summary>
        /// Registers a CorporationDataQuerying instance to be driven by this scheduler.
        /// </summary>
        public void Register(CorporationDataQuerying querying)
        {
            if (querying == null || _disposed)
                return;

            lock (_lock)
            {
                if (!_corporationQuerying.Contains(querying))
                    _corporationQuerying.Add(querying);
            }
        }

        /// <summary>
        /// Unregisters a CharacterDataQuerying instance.
        /// </summary>
        public void Unregister(CharacterDataQuerying querying)
        {
            if (querying == null)
                return;

            lock (_lock)
            {
                _characterQuerying.Remove(querying);
            }
        }

        /// <summary>
        /// Unregisters a CorporationDataQuerying instance.
        /// </summary>
        public void Unregister(CorporationDataQuerying querying)
        {
            if (querying == null)
                return;

            lock (_lock)
            {
                _corporationQuerying.Remove(querying);
            }
        }

        /// <summary>
        /// Gets the number of registered character querying instances.
        /// </summary>
        public int CharacterQueryingCount
        {
            get { lock (_lock) { return _characterQuerying.Count; } }
        }

        /// <summary>
        /// Gets the number of registered corporation querying instances.
        /// </summary>
        public int CorporationQueryingCount
        {
            get { lock (_lock) { return _corporationQuerying.Count; } }
        }

        /// <summary>
        /// Single tick handler that drives all registered querying instances.
        /// </summary>
        private void OnFiveSecondTick(object sender, EventArgs e)
        {
            if (_disposed)
                return;

            // Take snapshots under lock, then iterate outside lock to avoid holding
            // the lock during potentially long query operations.
            CharacterDataQuerying[] charSnapshot;
            CorporationDataQuerying[] corpSnapshot;

            lock (_lock)
            {
                charSnapshot = _characterQuerying.ToArray();
                corpSnapshot = _corporationQuerying.ToArray();
            }

            foreach (var querying in charSnapshot)
                querying.ProcessTick();

            foreach (var querying in corpSnapshot)
                querying.ProcessTick();

            // Drive ESIKey token refresh for all keys
            foreach (var key in EveMonClient.ESIKeys)
                key.ProcessTick();
        }

        /// <summary>
        /// Disposes the scheduler, unsubscribing from events.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            EveMonClient.FiveSecondTick -= OnFiveSecondTick;

            lock (_lock)
            {
                _characterQuerying.Clear();
                _corporationQuerying.Clear();
            }
        }
    }
}
