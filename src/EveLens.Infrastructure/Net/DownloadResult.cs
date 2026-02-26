// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EveLens.Common.Net
{
    /// <summary>
    /// Container class to return the result of an asynchronous string download
    /// </summary>
    public class DownloadResult<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DownloadResult{T}"/> class.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="error">The error.</param>
        /// <param name="response">The server response data.</param>
        public DownloadResult(T? result, HttpWebClientServiceException? error,
            ResponseParams? response = null)
        {
            Error = error;
            Result = result;
            Response = response ?? new ResponseParams(0);
        }

        /// <summary>
        /// Gets the response data.
        /// </summary>
        /// <value>The response data.</value>
        public ResponseParams Response { get; }

        /// <summary>
        /// Gets or sets the result.
        /// </summary>
        /// <value>The result.</value>
        public T? Result { get; }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        /// <value>The error.</value>
        public HttpWebClientServiceException? Error { get; }
    }
}
