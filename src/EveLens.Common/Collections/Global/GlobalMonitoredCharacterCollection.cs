// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Attributes;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;
using CommonEvents = EveLens.Common.Events;

namespace EveLens.Common.Collections.Global
{
    /// <summary>
    /// Represents the characters list
    /// </summary>
    [EnforceUIThreadAffinity]
    public sealed class GlobalMonitoredCharacterCollection : ReadonlyCollection<Character>
    {
        /// <summary>
        /// Update the order from the given list.
        /// </summary>
        /// <param name="order"></param>
        public void Update(IEnumerable<Character> order)
        {
            Items.Clear();
            Items.AddRange(order);

            // Notify the change
            AppServices.TraceService?.Trace("MonitoredCharactersChanged");
            AppServices.EventAggregator?.Publish(CommonEvents.MonitoredCharacterCollectionChangedEvent.Instance);
        }

        /// <summary>
        /// Moves the given character to the target index.
        /// </summary>
        /// <remarks>
        /// When the item is located before the target index, it is decremented. 
        /// That way we ensures the item is actually inserted before the item that originally was at <c>targetindex</c>.
        /// </remarks>
        /// <param name="item"></param>
        /// <param name="targetIndex"></param>
        public void MoveTo(Character item, int targetIndex)
        {
            int oldIndex = Items.IndexOf(item);
            if (oldIndex == -1)
                throw new InvalidOperationException("The item was not found in the collection.");

            if (oldIndex < targetIndex)
                targetIndex--;
            Items.RemoveAt(oldIndex);
            Items.Insert(targetIndex, item);

            AppServices.TraceService?.Trace("MonitoredCharactersChanged");
            AppServices.EventAggregator?.Publish(CommonEvents.MonitoredCharacterCollectionChangedEvent.Instance);
        }

        /// <summary>
        /// When the <see cref="Character.Monitored"/> property changed, this collection is updated.
        /// </summary>
        /// <param name="character">The character for which the property changed.</param>
        /// <param name="value"></param>
        internal void OnCharacterMonitoringChanged(Character character, bool value)
        {
            if (value)
            {
                if (Items.Contains(character))
                    return;

                Items.Add(character);
                AppServices.TraceService?.Trace("MonitoredCharactersChanged");
                AppServices.EventAggregator?.Publish(CommonEvents.MonitoredCharacterCollectionChangedEvent.Instance);
                return;
            }

            if (!Items.Contains(character))
                return;

            Items.Remove(character);
            AppServices.TraceService?.Trace("MonitoredCharactersChanged");
            AppServices.EventAggregator?.Publish(CommonEvents.MonitoredCharacterCollectionChangedEvent.Instance);
        }

        /// <summary>
        /// Imports the given characters.
        /// </summary>
        /// <param name="monitoredCharacters"></param>
        internal void Import(ICollection<MonitoredCharacterSettings> monitoredCharacters)
        {
            Items.Clear();

            foreach (MonitoredCharacterSettings characterSettings in monitoredCharacters)
            {
                Character character = AppServices.Characters[characterSettings.CharacterGuid.ToString()];
                if (character == null)
                    continue;

                Items.Add(character);
                character.Monitored = true;
                character.UISettings = characterSettings.Settings;
            }

            // Ensure every character is monitored — EveLens has no UI to unmonitor,
            // so unmonitored characters are ghosts from migrated EVEMon settings.
            // Full removal of Character.Monitored is tracked for 1.2.0.
            foreach (Character character in AppServices.Characters)
            {
                if (!character.Monitored && !Items.Contains(character))
                {
                    Items.Add(character);
                    character.Monitored = true;
                }
            }

            AppServices.TraceService?.Trace("MonitoredCharactersChanged");
            AppServices.EventAggregator?.Publish(CommonEvents.MonitoredCharacterCollectionChangedEvent.Instance);
        }

        /// <summary>
        /// Updates the settings from <see cref="Settings"/>. Adds and removes group as needed.
        /// </summary>
        internal IEnumerable<MonitoredCharacterSettings> Export()
            => Items.Select(character => new MonitoredCharacterSettings(character));
    }
}
