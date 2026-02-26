// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Linq;
using EveLens.Common.Constants;
using EveLens.Common.Data;
using EveLens.Common.Serialization.Esi;
using EveLens.Core;

namespace EveLens.Common.Models
{
    public sealed class ResearchPoint
    {
        #region Fields

        private readonly CCPCharacter m_character;
        private readonly float m_remainderPoints;
        private long m_stationID;

        #endregion


        #region Constructor

        /// <summary>
        /// Constructor from the API.
        /// </summary>
        /// <param name="src">The source item</param>
        /// <param name="character">The owning character</param>
        internal ResearchPoint(EsiResearchListItem src, CCPCharacter character)
        {
            GetAgentInfoByID(src.AgentID);

            AgentID = src.AgentID;
            Skill = StaticSkills.GetSkillByID(src.SkillID);
            StartDate = src.ResearchStartDate;
            PointsPerDay = src.PointsPerDay;
            m_character = character;
            m_remainderPoints = src.RemainderPoints;
            ResearchedItem = GetDatacore();
        }

        #endregion


        #region Properties

        /// <summary>
        /// Gets the agents ID.
        /// </summary>
        public int AgentID { get; }

        /// <summary>
        /// Gets the agents name.
        /// </summary>
        public string AgentName { get; private set; }

        /// <summary>
        /// Gets the agents level.
        /// </summary>
        public int AgentLevel { get; private set; }

        /// <summary>
        /// Gets the skill of research.
        /// </summary>
        public StaticSkill Skill { get; }

        /// <summary>
        /// Gets the agents field of research.
        /// </summary>
        public string Field => Skill.Name;

        /// <summary>
        /// Gets the research points per day.
        /// </summary>
        public double PointsPerDay { get; }

        /// <summary>
        /// Cets the current accumulated research points.
        /// </summary>
        public double CurrentRP => m_remainderPoints + PointsPerDay * DateTime.UtcNow.Subtract(StartDate).TotalDays;

        /// <summary>
        /// Gets the date the research was started.
        /// </summary>
        public DateTime StartDate { get; }

        /// <summary>
        /// Gets the station where the agent is.
        /// </summary>
        public Station Station { get; private set; }

        /// <summary>
        /// Gets the researched item.
        /// </summary>
        public Item ResearchedItem { get; }

        #endregion


        #region Helper Methods

        /// <summary>
        /// Assigns the supplemental info of an agent by its ID.
        /// </summary>
        /// <param name="id"></param>
        private void GetAgentInfoByID(int id)
        {
            Agent agent = StaticGeography.AllAgents.FirstOrDefault(x => x.ID == id);
            if (agent == null)
                return;

            AgentName = agent.Name;
            AgentLevel = agent.Level;

            if (agent.Station == null)
                return;

            m_stationID = agent.Station.ID;
            UpdateStation();
        }

        /// <summary>
        /// Gets the datacore this agent field researches.
        /// </summary>
        /// <returns></returns>
        private Item GetDatacore() => StaticItems.AllItems.FirstOrDefault(item =>
            item.MarketGroup.BelongsIn(DBConstants.DatacoresMarketGroupID) && item.
            Prerequisites.Any(prereq => prereq.Skill != null && prereq.Skill == Skill));

        #endregion


        #region Public Methods

        /// <summary>
        /// Updates the station.
        /// </summary>
        public void UpdateStation()
        {
            Station = ServiceLocator.StationResolver.GetStation(m_stationID, m_character?.CharacterID ?? 0) as Station;
        }

        #endregion
    }
}
