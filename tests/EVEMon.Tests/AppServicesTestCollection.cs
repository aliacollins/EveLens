// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using Xunit;

namespace EVEMon.Tests
{
    /// <summary>
    /// Defines a test collection for all test classes that mutate the shared static
    /// <see cref="EVEMon.Common.Services.AppServices"/> state. Tests within the same
    /// collection run sequentially, preventing race conditions from parallel execution
    /// that would corrupt the global ApplicationPaths, ServiceLocator, etc.
    /// </summary>
    [CollectionDefinition("AppServices")]
    public class AppServicesTestCollection
    {
        // This class has no code and is never instantiated.
        // Its purpose is to define the [CollectionDefinition] attribute
        // and associated test collection name.
    }
}
