// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EveLens.Common.Models
{
    /// <summary>
    /// Serializable class for an API method and its path. Each APIConfiguration maintains a list of APIMethodsEnum.
    /// </summary>
    public class APIMethod
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="method"></param>
        /// <param name="path"></param>
        private APIMethod(Enum method, string path)
        {
            Method = method;
            Path = path;
        }

        /// <summary>
        /// Returns the APIMethodsEnum enumeration member for this APIMethod.
        /// </summary>
        public Enum Method { get; }

        /// <summary>
        /// Returns the defined URL suffix path for this APIMethod.
        /// </summary>
        public string Path { get; set; }
    }
}
