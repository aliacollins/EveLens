// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Enumerations.CCPAPI;
using EVEMon.Common.Helpers;
using EVEMon.Common.Models;
using EVEMon.Common.Serialization.Esi;
using EVEMon.Common.Serialization.Eve;
using EVEMon.Common.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace EVEMon.Common.Service
{
    /// <summary>
    /// Centralized service for looking up structure information from ESI.
    /// Handles request deduplication, character rotation, and rate limiting.
    /// </summary>
    internal sealed class StructureLookupService
    {
        /// <summary>
        /// Maximum concurrent ESI requests.
        /// </summary>
        private const int MaxConcurrentRequests = 3;

        /// <summary>
        /// Cache of resolved structures (ID -> result).
        /// </summary>
        private readonly ConcurrentDictionary<long, SerializableOutpost> _cache;

        /// <summary>
        /// Currently pending requests (ID -> request state).
        /// </summary>
        private readonly ConcurrentDictionary<long, PendingStructureRequest> _pendingRequests;

        /// <summary>
        /// Semaphore to limit concurrent requests.
        /// </summary>
        private readonly SemaphoreSlim _requestSemaphore;

        /// <summary>
        /// Queue of IDs awaiting processing.
        /// </summary>
        private readonly ConcurrentQueue<long> _requestQueue;

        /// <summary>
        /// Is the processing loop running? (0 = no, 1 = yes)
        /// </summary>
        private volatile int _isProcessing;

        /// <summary>
        /// Counter for tracking request IDs in logs.
        /// </summary>
        private int _requestCounter;

        /// <summary>
        /// Event raised when structure data changes.
        /// </summary>
        public event EventHandler DataChanged;

        /// <summary>
        /// Creates a new structure lookup service.
        /// </summary>
        public StructureLookupService()
        {
            _cache = new ConcurrentDictionary<long, SerializableOutpost>();
            _pendingRequests = new ConcurrentDictionary<long, PendingStructureRequest>();
            _requestSemaphore = new SemaphoreSlim(MaxConcurrentRequests, MaxConcurrentRequests);
            _requestQueue = new ConcurrentQueue<long>();
            _isProcessing = 0;
        }

        /// <summary>
        /// Looks up structure information. Returns cached result immediately if available.
        /// Otherwise queues the request and returns null (caller should handle this case).
        /// </summary>
        /// <param name="structureId">The structure ID to look up.</param>
        /// <param name="requestingCharacter">The character making the request (used for key selection).</param>
        /// <returns>The structure info if cached, or null if lookup is in progress.</returns>
        public SerializableOutpost LookupStructure(long structureId, CCPCharacter requestingCharacter)
        {
            string charName = requestingCharacter?.Name ?? "unknown";

            // Check cache first
            if (_cache.TryGetValue(structureId, out var cached))
            {
                AppServices.TraceService?.Trace("StructureLookup [{0}] - CACHE HIT for {1:D} (requested by {2})",
                    _requestCounter, structureId, charName);
                return cached;
            }

            // Get or create pending request
            bool isNew = false;
            var request = _pendingRequests.GetOrAdd(structureId, id =>
            {
                isNew = true;
                return new PendingStructureRequest(id);
            });

            // If already completed (race condition), check result
            switch (request.State)
            {
                case StructureRequestState.Completed:
                    AppServices.TraceService?.Trace("StructureLookup [{0}] - Already completed for {1:D} (requested by {2})",
                        _requestCounter, structureId, charName);
                    return request.Result;

                case StructureRequestState.Inaccessible:
                case StructureRequestState.Destroyed:
                    AppServices.TraceService?.Trace("StructureLookup [{0}] - Already marked {1} for {2:D} (requested by {3})",
                        _requestCounter, request.State, structureId, charName);
                    return null;
            }

            // Log whether this is a new request or deduplicated
            if (isNew)
            {
                int counter = Interlocked.Increment(ref _requestCounter);
                AppServices.TraceService?.Trace("StructureLookup [{0}] - NEW REQUEST for {1:D} (requested by {2})",
                    counter, structureId, charName);
            }
            else
            {
                AppServices.TraceService?.Trace("StructureLookup [{0}] - DEDUPLICATED request for {1:D} (requested by {2}, state={3})",
                    _requestCounter, structureId, charName, request.State);
            }

            // Queue for processing if not already queued
            if (request.State == StructureRequestState.Pending)
            {
                _requestQueue.Enqueue(structureId);
                EnsureProcessingStarted();
            }

            return null; // Will be available after async processing
        }

        /// <summary>
        /// Async version that waits for the lookup to complete.
        /// </summary>
        /// <param name="structureId">The structure ID to look up.</param>
        /// <param name="requestingCharacter">The character making the request.</param>
        /// <returns>The structure info, or null if inaccessible/destroyed.</returns>
        public async Task<SerializableOutpost> LookupStructureAsync(long structureId, CCPCharacter requestingCharacter)
        {
            // Check cache first
            if (_cache.TryGetValue(structureId, out var cached))
                return cached;

            // Get or create pending request
            var request = _pendingRequests.GetOrAdd(structureId, id => new PendingStructureRequest(id));

            // Queue for processing if pending
            if (request.State == StructureRequestState.Pending)
            {
                _requestQueue.Enqueue(structureId);
                EnsureProcessingStarted();
            }

            // Wait for completion
            return await request.WaitAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Ensures the processing loop is running.
        /// </summary>
        private void EnsureProcessingStarted()
        {
            // Atomically check and set processing flag
            if (Interlocked.CompareExchange(ref _isProcessing, 1, 0) == 0)
            {
                // We won the race - start processing
                _ = ProcessQueueAsync();
            }
        }

        /// <summary>
        /// Main processing loop - processes queued requests with rate limiting.
        /// </summary>
        private async Task ProcessQueueAsync()
        {
            try
            {
                while (_requestQueue.TryDequeue(out long structureId))
                {
                    // Check rate limiting
                    if (EsiErrors.IsErrorCountExceeded)
                    {
                        // Wait until error count resets
                        var delay = EsiErrors.ErrorCountResetTime - DateTime.UtcNow;
                        if (delay > TimeSpan.Zero && delay < TimeSpan.FromMinutes(5))
                        {
                            AppServices.TraceService?.Trace("StructureLookupService - Rate limited, waiting {0:F0}s",
                                delay.TotalSeconds);
                            await Task.Delay(delay).ConfigureAwait(false);
                        }
                    }

                    // Acquire semaphore slot
                    await _requestSemaphore.WaitAsync().ConfigureAwait(false);

                    // Process in background, don't await - let multiple requests run in parallel
                    _ = ProcessSingleRequestAsync(structureId);
                }
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace("StructureLookupService - Queue processing error: {0}", ex.Message);
                ExceptionHandler.LogException(ex, false);
            }
            finally
            {
                // Reset processing flag
                Interlocked.Exchange(ref _isProcessing, 0);

                // Check if more items were added while we were finishing
                if (!_requestQueue.IsEmpty)
                    EnsureProcessingStarted();
            }
        }

        /// <summary>
        /// Processes a single structure request, trying multiple characters if needed.
        /// </summary>
        private async Task ProcessSingleRequestAsync(long structureId)
        {
            try
            {
                if (!_pendingRequests.TryGetValue(structureId, out var request))
                    return;

                // Skip if already completed
                if (request.State != StructureRequestState.Pending &&
                    request.State != StructureRequestState.InProgress)
                    return;

                request.SetInProgress();

                // Get all characters with CitadelInfo access
                var candidateCharacters = GetCharactersWithCitadelAccess().ToList();
                var untriedCharacters = request.GetUntriedCharacters(candidateCharacters).ToList();

                AppServices.TraceService?.Trace("StructureLookup - Processing {0:D}: {1} candidates, {2} untried",
                    structureId, candidateCharacters.Count, untriedCharacters.Count);

                foreach (var character in untriedCharacters)
                {
                    var esiKey = character.Identity?.FindAPIKeyWithAccess(
                        ESIAPICharacterMethods.CitadelInfo);

                    if (esiKey == null || string.IsNullOrEmpty(esiKey.AccessToken))
                        continue;

                    // Mark this character as tried
                    if (!request.TryMarkCharacterAttempted(character.CharacterID))
                        continue;

                    AppServices.TraceService?.Trace("ESI structure lookup for {0:D} using {1}",
                        structureId, character.Name);

                    try
                    {
                        // Make the ESI request
                        var result = await AppServices.APIProviders.CurrentProvider
                            .QueryEsiAsync<EsiAPIStructure>(
                                ESIAPIGenericMethods.CitadelInfo,
                                new ESIParams(null, esiKey.AccessToken)
                                {
                                    ParamOne = structureId
                                })
                            .ConfigureAwait(false);

                        if (!result.HasError)
                        {
                            // Success!
                            var outpost = result.Result.ToXMLItem(structureId);
                            _cache[structureId] = outpost;
                            request.Complete(outpost);

                            AppServices.TraceService?.Trace("StructureLookup - SUCCESS for {0:D} = \"{1}\" (using {2})",
                                structureId, outpost.StationName, character.Name);

                            // Notify on UI thread
                            OnDataChanged();
                            return;
                        }

                        // Handle specific error codes
                        if (result.ResponseCode == (int)HttpStatusCode.NotFound)
                        {
                            // Structure destroyed
                            request.SetDestroyed();
                            AppServices.TraceService?.Trace("Structure {0:D} destroyed (404)", structureId);
                            return;
                        }

                        if (result.ResponseCode == (int)HttpStatusCode.Forbidden)
                        {
                            // This character doesn't have access - try next character
                            AppServices.TraceService?.Trace("Character {0} lacks access to structure {1:D}",
                                character.Name, structureId);
                            continue;
                        }

                        // Other error - log but continue trying other characters
                        AppServices.TraceService?.Trace("ESI error for structure {0:D}: {1}",
                            structureId, result.ErrorMessage);
                    }
                    catch (Exception ex)
                    {
                        AppServices.TraceService?.Trace("Exception looking up structure {0:D}: {1}",
                            structureId, ex.Message);
                    }
                }

                // All characters tried, none succeeded
                request.SetInaccessible();
                AppServices.TraceService?.Trace("Structure {0:D} is inaccessible to all {1} characters",
                    structureId, request.TriedCharacterCount);
            }
            finally
            {
                _requestSemaphore.Release();
            }
        }

        /// <summary>
        /// Gets all CCP characters that have CitadelInfo access.
        /// Prioritizes monitored characters.
        /// </summary>
        private IEnumerable<CCPCharacter> GetCharactersWithCitadelAccess()
        {
            // Get all CCP characters
            var allCharacters = AppServices.Characters.OfType<CCPCharacter>()
                .Where(c => c.Identity?.FindAPIKeyWithAccess(ESIAPICharacterMethods.CitadelInfo) != null)
                .ToList();

            // Prioritize monitored characters (more likely to have relevant access)
            var monitored = allCharacters.Where(c => c.Monitored).ToList();
            var unmonitored = allCharacters.Except(monitored);

            return monitored.Concat(unmonitored);
        }

        /// <summary>
        /// Raises the DataChanged event on the UI thread.
        /// </summary>
        private void OnDataChanged()
        {
            AppServices.Dispatcher?.Invoke(() =>
            {
                AppServices.Notifications.InvalidateAPIError();
                DataChanged?.Invoke(this, EventArgs.Empty);
            });
        }

        /// <summary>
        /// Imports cached data from file.
        /// </summary>
        /// <param name="stations">The stations to import.</param>
        public void ImportFromCache(IEnumerable<SerializableOutpost> stations)
        {
            foreach (var station in stations)
            {
                if (station != null)
                    _cache[station.StationID] = station;
            }
        }

        /// <summary>
        /// Exports cache data for persistence.
        /// </summary>
        /// <returns>All cached stations.</returns>
        public IEnumerable<SerializableOutpost> ExportCache()
        {
            return _cache.Values.Where(v => v != null).ToList();
        }

        /// <summary>
        /// Checks if a structure is in the cache.
        /// </summary>
        /// <param name="structureId">The structure ID.</param>
        /// <returns>True if cached.</returns>
        public bool IsCached(long structureId)
        {
            return _cache.ContainsKey(structureId);
        }

        /// <summary>
        /// Gets the count of cached structures.
        /// </summary>
        public int CacheCount => _cache.Count;

        /// <summary>
        /// Gets the count of pending requests.
        /// </summary>
        public int PendingCount => _pendingRequests.Count(r =>
            r.Value.State == StructureRequestState.Pending ||
            r.Value.State == StructureRequestState.InProgress);
    }
}
