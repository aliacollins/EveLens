// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using EveLens.Core.Interfaces;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Persists live ESI response data to disk as per-endpoint JSON files.
    /// Files are stored at: {DataDirectory}/cache/characters/{characterId}/{endpointKey}.json
    /// </summary>
    /// <remarks>
    /// Uses atomic writes (write to .tmp then rename) to prevent corruption.
    /// Save is fire-and-forget from callers; Load is awaited on startup.
    /// </remarks>
    internal sealed class CharacterDataCacheService : ICharacterDataCache
    {
        private static readonly JsonSerializerOptions s_options = new JsonSerializerOptions
        {
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };

        private static readonly JsonSerializerOptions s_readOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        private string GetCacheRoot()
        {
            string dataDir = AppServices.ApplicationPaths.DataDirectory;
            return Path.Combine(dataDir, "cache", "characters");
        }

        private string GetCharacterDir(long characterId)
        {
            return Path.Combine(GetCacheRoot(), characterId.ToString());
        }

        private string GetFilePath(long characterId, string endpointKey)
        {
            return Path.Combine(GetCharacterDir(characterId), endpointKey + ".json");
        }

        public async Task SaveAsync<T>(long characterId, string endpointKey, T data)
        {
            try
            {
                string dir = GetCharacterDir(characterId);
                Directory.CreateDirectory(dir);

                string filePath = GetFilePath(characterId, endpointKey);
                string tmpPath = filePath + ".tmp";

                byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(data, s_options);
                await File.WriteAllBytesAsync(tmpPath, bytes).ConfigureAwait(false);
                File.Move(tmpPath, filePath, overwrite: true);
            }
            catch (Exception ex)
            {
                // Cache save failure is non-critical — log and continue
                AppServices.TraceService?.Trace(
                    $"CharacterDataCache.SaveAsync failed for {characterId}/{endpointKey}: {ex.Message}");
            }
        }

        public async Task<T?> LoadAsync<T>(long characterId, string endpointKey) where T : class
        {
            try
            {
                string filePath = GetFilePath(characterId, endpointKey);
                if (!File.Exists(filePath))
                    return null;

                byte[] bytes = await File.ReadAllBytesAsync(filePath).ConfigureAwait(false);
                return JsonSerializer.Deserialize<T>(bytes, s_readOptions);
            }
            catch (Exception ex)
            {
                // Cache load failure is non-critical — log and return null
                AppServices.TraceService?.Trace(
                    $"CharacterDataCache.LoadAsync failed for {characterId}/{endpointKey}: {ex.Message}");
                return null;
            }
        }

        public Task ClearEndpointAsync(long characterId, string endpointKey)
        {
            try
            {
                string filePath = GetFilePath(characterId, endpointKey);
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"CharacterDataCache.ClearEndpointAsync failed for {characterId}/{endpointKey}: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public Task ClearCharacterAsync(long characterId)
        {
            try
            {
                string dir = GetCharacterDir(characterId);
                if (Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"CharacterDataCache.ClearCharacterAsync failed for {characterId}: {ex.Message}");
            }
            return Task.CompletedTask;
        }

        public Task ClearAllAsync()
        {
            try
            {
                string root = GetCacheRoot();
                if (Directory.Exists(root))
                    Directory.Delete(root, recursive: true);
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"CharacterDataCache.ClearAllAsync failed: {ex.Message}");
            }
            return Task.CompletedTask;
        }
    }
}
