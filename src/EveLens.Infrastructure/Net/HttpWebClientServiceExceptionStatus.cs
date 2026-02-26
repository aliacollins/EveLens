// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Net
{
    /// <summary>
    /// Status types for an HttpWebServiceException
    /// </summary>
    public enum HttpWebClientServiceExceptionStatus
    {
        Exception,
        WebException,
        RedirectsExceeded,
        RequestsDisabled,
        ServerError,
        ProxyError,
        NameResolutionFailure,
        ConnectFailure,
        Timeout,
        XmlException,
        ImageException,
        FileError,
        Forbidden
    }
}
