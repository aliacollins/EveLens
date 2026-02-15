using System;
using System.Xml.Serialization;
using EVEMon.Common.Extensions;
using EVEMon.Common.Serialization.Settings;

namespace EVEMon.Common.Models
{
    /// <summary>
    /// Represents a booster injection point attached to a plan entry.
    /// Marks where a user plans to inject a cerebral accelerator (booster).
    /// </summary>
    public sealed class BoosterPoint
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public BoosterPoint()
        {
            Guid = Guid.NewGuid();
        }

        /// <summary>
        /// Constructor with values.
        /// </summary>
        /// <param name="bonus">The booster bonus to all attributes (1-12).</param>
        /// <param name="durationHours">The booster duration in hours.</param>
        public BoosterPoint(int bonus, int durationHours)
            : this()
        {
            Bonus = bonus;
            DurationHours = durationHours;
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="serial">The serialized data.</param>
        /// <exception cref="System.ArgumentNullException">serial</exception>
        public BoosterPoint(SerializableBoosterPoint serial)
        {
            serial.ThrowIfNull(nameof(serial));

            Guid = Guid.NewGuid();
            Bonus = serial.Bonus;
            DurationHours = serial.DurationHours;
        }

        /// <summary>
        /// Gets a global identifier of this booster point.
        /// </summary>
        [XmlIgnore]
        public Guid Guid { get; private set; }

        /// <summary>
        /// Gets or sets the booster bonus to all attributes (typically 1-12).
        /// </summary>
        public int Bonus { get; set; }

        /// <summary>
        /// Gets or sets the booster duration in hours.
        /// </summary>
        public int DurationHours { get; set; }

        /// <summary>
        /// Gets the booster duration as a TimeSpan.
        /// </summary>
        [XmlIgnore]
        public TimeSpan Duration => TimeSpan.FromHours(DurationHours);

        /// <summary>
        /// Gets a string representation of this booster point.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Booster: +{Bonus} to all attributes for {DurationHours} hours";
        }

        /// <summary>
        /// Gets a short string representation for display in the plan list.
        /// </summary>
        /// <returns></returns>
        public string ToShortString()
        {
            return $"+{Bonus} ({DurationHours}h)";
        }

        /// <summary>
        /// Gets a hash code from the GUID.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => Guid.GetHashCode();

        /// <summary>
        /// Clones the booster point.
        /// </summary>
        /// <returns></returns>
        public BoosterPoint Clone()
        {
            return new BoosterPoint
            {
                Bonus = Bonus,
                DurationHours = DurationHours,
                Guid = Guid
            };
        }

        /// <summary>
        /// Creates a serialization object.
        /// </summary>
        /// <returns></returns>
        internal SerializableBoosterPoint Export() => new SerializableBoosterPoint
        {
            Bonus = Bonus,
            DurationHours = DurationHours
        };
    }
}
