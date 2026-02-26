// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Collections;
using EveLens.Common.Enumerations;
using EveLens.Common.Enumerations.CCPAPI;
using EveLens.Common.Interfaces;
using EveLens.Common.Serialization.Esi;
using EveLens.Common.Services;
using CommonEvents = EveLens.Common.Events;

namespace EveLens.Common.Models.Collections
{
    public sealed class PlanetaryColonyCollection : ReadonlyCollection<PlanetaryColony>
    {
        private readonly CCPCharacter m_ccpCharacter;


        #region Constructor

        /// <summary>
        /// Internal constructor.
        /// </summary>
        /// <param name="ccpCharacter">The CCP character.</param>
        internal PlanetaryColonyCollection(CCPCharacter ccpCharacter)
        {
            m_ccpCharacter = ccpCharacter;

            m_ccpCharacter.Services.FiveSecondTick += EveLensClient_TimerTick;
        }

        #endregion


        #region Inherited Events

        /// <summary>
        /// Called when the object gets disposed.
        /// </summary>
        internal void Dispose()
        {
            m_ccpCharacter.Services.FiveSecondTick -= EveLensClient_TimerTick;
        }

        /// <summary>
        /// Handles the TimerTick event of the EveLensClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void EveLensClient_TimerTick(object sender, EventArgs e)
        {
            IQueryMonitor charPlanetaryColoniesMonitor = m_ccpCharacter.QueryMonitors[ESIAPICharacterMethods.PlanetaryColonies];

            if (charPlanetaryColoniesMonitor == null || !charPlanetaryColoniesMonitor.Enabled)
                return;

            UpdateOnTimerTick();
        }

        #endregion


        #region Importation

        /// <summary>
        /// Imports an enumeration of API objects.
        /// </summary>
        /// <param name="src">The enumeration of serializable planetary colony log from the API.</param>
        internal void Import(IEnumerable<EsiPlanetaryColonyListItem> src)
        {
            Items.Clear();

            // Import the palnetary colony from the API
            foreach (EsiPlanetaryColonyListItem srcColony in src)
            {
                Items.Add(new PlanetaryColony(m_ccpCharacter, srcColony));
            }
        }

        #endregion


        #region Helper Methods

        /// <summary>
        /// Notify the user on a pin completion.
        /// </summary>
        private void UpdateOnTimerTick()
        {
            // We exit if there are no pins
            if (!Items.Any())
                return;

            // Add the not notified idle pins to the completed list
            var pinsCompleted = Items.SelectMany(x => x.Pins).Where(pin => pin.State !=
                PlanetaryPinState.None && pin.TTC.Length == 0 && !pin.NotificationSend).ToList();

            pinsCompleted.ForEach(pin => pin.NotificationSend = true);

            // We exit if no pins have finished
            if (!pinsCompleted.Any())
                return;

            // Fires the event regarding the character's pins finished
            AppServices.TraceService?.Trace(m_ccpCharacter.Name);
            m_ccpCharacter.OnPlanetaryPinsCompleted(pinsCompleted);
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterPlanetaryPinsCompletedEvent(m_ccpCharacter, pinsCompleted));
        }

        #endregion
    }
}
