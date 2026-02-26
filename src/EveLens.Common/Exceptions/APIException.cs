// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Serialization;

namespace EveLens.Common.Exceptions
{
    internal class APIException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="APIException"/> class.
        /// </summary>
        /// <param name="error">The error.</param>
        public APIException(SerializableAPIError error)
            : base(error.ErrorMessage)
        {
            ErrorCode = error.ErrorCode;
        }

        /// <summary>
        /// Gets or sets the error code.
        /// </summary>
        /// <value>
        /// The error code.
        /// </value>
        public string ErrorCode { get; }
    }
}