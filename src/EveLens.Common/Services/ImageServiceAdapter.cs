// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Threading.Tasks;
using EveLens.Common.Service;

namespace EveLens.Common.Services
{
    public sealed class ImageServiceAdapter : Core.Interfaces.IImageService
    {
        public async Task<object?> GetImageAsync(Uri url, bool useCache = true)
        {
            return await ImageService.GetImageAsync(url, useCache);
        }
    }
}
