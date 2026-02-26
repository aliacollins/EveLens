// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using SkiaSharp;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EveLens.Common.Enumerations;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Extensions;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;
using EveLens.Core.Enumerations;

namespace EveLens.Common.Helpers
{
    /// <summary>
    /// Saves a couple of repetitive tasks.
    /// </summary>
    public static class UIHelper
    {
        public static SKBitmap? CharacterMonitorScreenshot { get; set; }

        /// <summary>
        /// Saves the plans to a file.
        /// </summary>
        /// <param name="plans">The plans.</param>
        public static async Task SavePlansAsync(IList<Plan> plans)
        {
            Character character = (Character)plans.First().Character;

            // Prompt the user to pick a file name
            string? filePath = AppServices.DialogService.ShowSaveDialog(
                @"Save to File",
                @"EveLens Plans Backup Format (*.epb)|*.epb",
                $"{character.Name} - Plans Backup");

            if (filePath == null)
                return;

            try
            {
                string content = PlanIOHelper.ExportAsXML(plans);

                // Moves to the final file
                await FileHelper.OverwriteOrWarnTheUserAsync(
                    filePath,
                    async fs =>
                        {
                            // Emp is actually compressed xml
                            Stream stream = new GZipStream(fs, CompressionMode.Compress);
                            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
                            {
                                await writer.WriteAsync(content);
                                await writer.FlushAsync();
                                await stream.FlushAsync();
                                await fs.FlushAsync();
                            }
                            return true;
                        });
            }
            catch (IOException err)
            {
                ExceptionHandler.LogException(err, false);
                AppServices.DialogService.ShowMessage(
                    $"There was an error writing out the file:\n\n{err.Message}",
                    @"Save Failed", DialogButtons.OK, DialogIcon.Error);
            }
        }

        /// <summary>
        /// Displays the plan exportation window and then exports it.
        /// </summary>
        /// <param name="plan">The plan.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static async Task ExportPlanAsync(Plan plan)
        {
            plan.ThrowIfNull(nameof(plan));

            await ExportPlanAsync(plan, (Character)plan.Character);
        }

        /// <summary>
        /// Exports the character's selected skills as plan.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="selectedSkills">The selected skills.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public static async Task ExportCharacterSkillsAsPlanAsync(Character character, IEnumerable<Skill> selectedSkills = null)
        {
            character.ThrowIfNull(nameof(character));

            // Create a character without any skill
            CharacterScratchpad scratchpad = new CharacterScratchpad(character);
            scratchpad.ClearSkills();

            // Create a new plan
            Plan plan = new Plan(scratchpad) { Name = "Skills Plan" };

            IEnumerable<Skill> skills = selectedSkills ?? character.Skills.Where(skill => skill.IsPublic);

            // Add all trained skill levels that the character has trained so far
            foreach (Skill skill in skills)
            {
                plan.PlanTo(skill, skill.Level);
            }

            await ExportPlanAsync(plan, character);
        }

        /// <summary>
        /// Displays the plan exportation window and then exports it.
        /// </summary>
        /// <param name="plan">The plan.</param>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.NotImplementedException"></exception>
        private static async Task ExportPlanAsync(Plan plan, Character character)
        {
            plan.ThrowIfNull(nameof(plan));

            character.ThrowIfNull(nameof(character));

            // Assemble an initial filename and remove prohibited characters
            string planSaveName = $"{character.Name} - {plan.Name}";
            char[] invalidFileChars = Path.GetInvalidFileNameChars();
            int fileInd = planSaveName.IndexOfAny(invalidFileChars);
            while (fileInd != -1)
            {
                planSaveName = planSaveName.Replace(planSaveName[fileInd], '-');
                fileInd = planSaveName.IndexOfAny(invalidFileChars);
            }

            // Prompt the user to pick a file name
            string? filePath = AppServices.DialogService.ShowSaveDialog(
                @"Save to File",
                @"EveLens Plan Format (*.emp)|*.emp|XML  Format (*.xml)|*.xml|Text Format (*.txt)|*.txt",
                planSaveName);

            if (filePath == null)
                return;

            // Serialize
            try
            {
                // Determine format from file extension
                string ext = Path.GetExtension(filePath).ToLowerInvariant();
                PlanFormat format = ext switch
                {
                    ".xml" => PlanFormat.Xml,
                    ".txt" => PlanFormat.Text,
                    _ => PlanFormat.Emp, // .emp or any other
                };

                string content;
                switch (format)
                {
                    case PlanFormat.Emp:
                    case PlanFormat.Xml:
                        content = PlanIOHelper.ExportAsXML(plan);
                        break;
                    case PlanFormat.Text:
                        // Prompts the user and returns if canceled
                        PlanExportSettings settings = PromptUserForPlanExportSettings(plan);
                        if (settings == null)
                            return;

                        content = PlanIOHelper.ExportAsText(plan, settings);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                // Moves to the final file
                await FileHelper.OverwriteOrWarnTheUserAsync(
                    filePath,
                    async fs =>
                        {
                            Stream stream = fs;
                            // Emp is actually compressed text
                            if (format == PlanFormat.Emp)
                                stream = new GZipStream(fs, CompressionMode.Compress);

                            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
                            {
                                await writer.WriteAsync(content);
                                await writer.FlushAsync();
                                await stream.FlushAsync();
                                await fs.FlushAsync();
                            }
                            return true;
                        });
            }
            catch (IOException err)
            {
                ExceptionHandler.LogException(err, true);
                AppServices.DialogService.ShowMessage(
                    $"There was an error writing out the file:\n\n{err.Message}",
                    @"Save Failed", DialogButtons.OK, DialogIcon.Error);
            }
        }

        /// <summary>
        /// Prompt the user to select plan exportation settings.
        /// Uses current saved settings directly (cross-platform; no WinForms dialog).
        /// </summary>
        /// <returns></returns>
        public static PlanExportSettings PromptUserForPlanExportSettings(Plan plan)
        {
            PlanExportSettings settings = Settings.Exportation.PlanToText;

            if (settings.Markup == MarkupType.Undefined)
                settings.Markup = MarkupType.None;

            return settings;
        }

        /// <summary>
        /// Displays the character exportation window and then exports it.
        /// Optionally it exports it as it would be after the plan finish.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="plan">The plan.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">character</exception>
        public static async Task ExportCharacterAsync(Character character, Plan plan = null)
        {
            character.ThrowIfNull(nameof(character));

            bool isAfterPlanExport = plan != null;

            string filter = @"Text Format|*.txt|CHR Format (EFT)|*.chr|HTML Format|*.html|XML Format (EveLens)|*.xml";
            if (!isAfterPlanExport)
                filter += @"|XML Format (CCP API)|*.xml|PNG Image|*.png";

            string defaultName = $"{character.Name}{(isAfterPlanExport ? $" (after plan {plan.Name})" : string.Empty)}";

            string? filePath = AppServices.DialogService.ShowSaveDialog(
                $"Save {(isAfterPlanExport ? "After Plan " : string.Empty)}Character Info",
                filter,
                defaultName);

            if (filePath == null)
                return;

            // Serialize
            try
            {
                // Determine format from file extension
                string ext = Path.GetExtension(filePath).ToLowerInvariant();
                CharacterSaveFormat format = ext switch
                {
                    ".txt" => CharacterSaveFormat.Text,
                    ".chr" => CharacterSaveFormat.EFTCHR,
                    ".html" => CharacterSaveFormat.HTML,
                    ".png" => CharacterSaveFormat.PNG,
                    ".xml" => isAfterPlanExport ? CharacterSaveFormat.EveLensXML : CharacterSaveFormat.CCPXML,
                    _ => CharacterSaveFormat.Text,
                };

                // Save character with the chosen format to our file
                await FileHelper.OverwriteOrWarnTheUserAsync(
                    filePath,
                    async fs =>
                        {
                            if (format == CharacterSaveFormat.PNG)
                            {
                                SKBitmap? image = CharacterMonitorScreenshot;
                                if (image != null)
                                {
                                    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                                    data.SaveTo(fs);
                                }
                                await fs.FlushAsync();
                                return true;
                            }

                            string content = CharacterExporter.Export(format, character, plan);
                            if ((format == CharacterSaveFormat.CCPXML) && string.IsNullOrEmpty(content))
                            {
                                AppServices.DialogService.ShowMessage(
                                    @"This character has never been downloaded from CCP, cannot find it in the XML cache.",
                                    @"Cannot export the character", DialogButtons.OK, DialogIcon.Warning);
                                return false;
                            }

                            using (StreamWriter sw = new StreamWriter(fs))
                            {
                                await sw.WriteAsync(content);
                                await sw.FlushAsync();
                                await fs.FlushAsync();
                            }
                            return true;
                        });
            }
                // Handle exception
            catch (IOException exc)
            {
                ExceptionHandler.LogException(exc, true);
                AppServices.DialogService.ShowMessage(
                    @"A problem occurred during exportation. The operation has not been completed.",
                    @"Export Failed", DialogButtons.OK, DialogIcon.Error);
            }
        }

        /// <summary>
        /// Shows a no support message.
        /// </summary>
        /// <returns></returns>
        internal static object ShowNoSupportMessage()
        {
            AppServices.DialogService.ShowMessage(
                $"The file is probably from an EveLens version prior to 1.3.0.{Environment.NewLine}" +
                @"This type of file is no longer supported.",
                @"File type not supported", DialogButtons.OK, DialogIcon.Information);

            return null;
        }
    }
}
