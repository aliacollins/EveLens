// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Extensions;
using EveLens.Common.Serialization.Datafiles;
using System;

namespace EveLens.Common.Data
{
    /// <summary>
    /// Represents a planet inside the EVE universe.
    /// </summary>
    public class Planet : IComparable<Planet>
    {

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="SolarSystem"/> class.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <param name="src">The source.</param>
        /// <exception cref="System.ArgumentNullException">owner or src</exception>
        public Planet(SolarSystem owner, SerializablePlanet src)
        {
            owner.ThrowIfNull(nameof(owner));
            src.ThrowIfNull(nameof(src));

            SolarSystem = owner;
            ID = src.ID;
            Name = src.Name;
            TypeID = src.TypeID;
        }

        #endregion


        #region Helper Methods

        /// <summary>
        /// Compares this planet with another one.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">other</exception>
        public int CompareTo(Planet other)
        {
            other.ThrowIfNull(nameof(other));

            return SolarSystem != other.SolarSystem
                ? SolarSystem.CompareTo(other.SolarSystem)
                : string.Compare(Name, other.Name, StringComparison.CurrentCulture);
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets this object's id.
        /// </summary>
        public int ID { get; }

        /// <summary>
        /// Gets this object's name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets this object's parent solar system.
        /// </summary>
        public SolarSystem SolarSystem { get; }

        /// <summary>
        /// Gets this object's type ID.
        /// </summary>
        public int TypeID { get; }

        #endregion


        #region Overridden Methods

        public override string ToString() => Name;

        #endregion

    }
}
