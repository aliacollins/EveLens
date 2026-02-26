// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using EveLens.Common.Data;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Core;
using EveLens.Core.Enumerations;
using EveLens.Core.Interfaces;
using NSubstitute;
using Xunit;

namespace EveLens.Tests.TestDoubles
{
    /// <summary>
    /// Provides static skill data loading and helper methods for Plan model tests.
    /// Loads <c>eve-skills-en-US.xml.gzip</c> from the test output <c>Resources/</c> folder
    /// so that <see cref="StaticSkills"/>, <see cref="Plan"/>, and <see cref="PlanEntry"/>
    /// can be exercised with real game data.
    /// </summary>
    /// <remarks>
    /// Uses a lock + flag to ensure <see cref="StaticSkills.Load()"/> runs exactly once
    /// across all tests in the <c>"StaticData"</c> collection.
    /// Tests that use this fixture must be decorated with <c>[Collection("StaticData")]</c>.
    /// </remarks>
    public static class PlanTestFixture
    {
        private static readonly object s_lock = new object();
        private static bool s_initialized;

        /// <summary>
        /// Ensures static skill data is loaded. Safe to call multiple times;
        /// only the first call performs the actual load.
        /// </summary>
        public static void EnsureStaticSkillsLoaded()
        {
            if (s_initialized)
                return;

            lock (s_lock)
            {
                if (s_initialized)
                    return;

                // Create a temp directory for the DataDirectory cache path
                string tempDataDir = Path.Combine(Path.GetTempPath(), "EveLens_Tests_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDataDir);

                // Set up IApplicationPaths with the temp dir
                var paths = Substitute.For<IApplicationPaths>();
                paths.DataDirectory.Returns(tempDataDir);
                paths.XmlCacheDirectory.Returns(Path.Combine(tempDataDir, "cache"));
                paths.ImageCacheDirectory.Returns(Path.Combine(tempDataDir, "images"));
                paths.SettingsFilePath.Returns(Path.Combine(tempDataDir, "settings.json"));
                paths.TraceFilePath.Returns(Path.Combine(tempDataDir, "trace.log"));

                // Set up IResourceProvider with real XSLT from embedded resources
                var resourceProvider = Substitute.For<IResourceProvider>();
                resourceProvider.DatafilesXSLT.Returns(Common.Properties.Resources.DatafilesXSLT);
                resourceProvider.ChrFactions.Returns(string.Empty);

                // Set up IDialogService to prevent UI dialogs
                var dialogService = Substitute.For<IDialogService>();
                dialogService.ShowMessage(Arg.Any<string>(), Arg.Any<string>(),
                    Arg.Any<DialogButtons>(), Arg.Any<DialogIcon>())
                    .Returns(DialogChoice.OK);

                // Configure AppServices
                AppServices.SetApplicationPaths(paths);
                AppServices.SetResourceProvider(resourceProvider);
                AppServices.SetDialogService(dialogService);

                // Sync to ServiceLocator so StaticSkills.Load() can find ResourceProvider
                AppServices.SyncToServiceLocator();

                // Load the static skill data
                StaticSkills.Load();

                s_initialized = true;
            }
        }

        /// <summary>
        /// Gets a <see cref="StaticSkill"/> by name. Throws if not found.
        /// </summary>
        public static StaticSkill GetSkill(string name)
        {
            EnsureStaticSkillsLoaded();
            var skill = StaticSkills.GetSkillByName(name);
            if (skill == null)
                throw new InvalidOperationException($"Static skill '{name}' not found. Ensure skill data is loaded.");
            return skill;
        }

        /// <summary>
        /// Gets a <see cref="StaticSkill"/> by its EVE ID. Throws if not found.
        /// </summary>
        public static StaticSkill GetSkillByID(long id)
        {
            EnsureStaticSkillsLoaded();
            var skill = StaticSkills.GetSkillByID(id);
            if (skill == null)
                throw new InvalidOperationException($"Static skill with ID {id} not found. Ensure skill data is loaded.");
            return skill;
        }

        /// <summary>
        /// Creates a test <see cref="CCPCharacter"/> with no external dependencies.
        /// </summary>
        public static CCPCharacter CreateTestCharacter(string name = "Test Pilot", long id = 99999L)
        {
            EnsureStaticSkillsLoaded();
            var services = new NullCharacterServices();
            var identity = new CharacterIdentity(id, name);
            return new CCPCharacter(identity, services);
        }

        /// <summary>
        /// Creates a <see cref="Plan"/> for the given character.
        /// </summary>
        public static Plan CreateTestPlan(BaseCharacter character, string name = "Test Plan")
        {
            EnsureStaticSkillsLoaded();
            var plan = new Plan(character) { Name = name };
            return plan;
        }

        /// <summary>
        /// Creates a <see cref="Plan"/> with a fresh test character.
        /// </summary>
        public static Plan CreateTestPlanWithCharacter(string planName = "Test Plan", string characterName = "Test Pilot")
        {
            var character = CreateTestCharacter(characterName);
            return CreateTestPlan(character, planName);
        }
    }

    /// <summary>
    /// Defines the "StaticData" test collection. Tests in this collection run sequentially
    /// to avoid race conditions during static skill data loading.
    /// </summary>
    [CollectionDefinition("StaticData")]
    public class StaticDataTestCollection
    {
        // This class has no code and is never instantiated.
        // Its purpose is to define the [CollectionDefinition] attribute.
    }
}
