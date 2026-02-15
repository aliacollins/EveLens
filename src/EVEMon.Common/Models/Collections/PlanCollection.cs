using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Collections;
using EVEMon.Common.Extensions;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.Services;
using CommonEvents = EVEMon.Common.Events;

namespace EVEMon.Common.Models.Collections
{
    /// <summary>
    /// Represents the character's list of plans.
    /// </summary>
    public sealed class PlanCollection : BaseList<Plan>
    {
        private readonly Character m_owner;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal PlanCollection(Character owner)
        {
            m_owner = owner;
        }

        /// <summary>
        /// Gets the plan with the given name, null when not found.
        /// </summary>
        /// <value>
        /// The <see cref="Plan" />.
        /// </value>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public Plan this[string name] => Items.FirstOrDefault(plan => plan.Name == name);

        /// <summary>
        /// When we add a plan, we may have to clone it (and maybe changed the character it is bound to) and connects it.
        /// </summary>
        /// <param name="item"></param>
        /// <exception cref="System.ArgumentNullException">item</exception>
        protected override void OnAdding(ref Plan item)
        {
            item.ThrowIfNull(nameof(item));

            if (item.Character != m_owner)
                item = item.Clone(m_owner);
            else if (Contains(item))
                item = item.Clone();

            item.IsConnected = true;
        }

        /// <summary>
        /// When removing a plan, we need to disconnect it.
        /// </summary>
        /// <param name="oldItem"></param>
        /// <exception cref="System.ArgumentNullException">oldItem</exception>
        protected override void OnRemoving(Plan oldItem)
        {
            oldItem.ThrowIfNull(nameof(oldItem));

            oldItem.IsConnected = false;
        }

        /// <summary>
        /// When the collection changed, the global event is fired.
        /// </summary>
        protected override void OnChanged()
        {
            AppServices.TraceService?.Trace(m_owner?.Name);
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterPlanCollectionChangedEvent(m_owner));
        }

        /// <summary>
        /// Imports data from the given deserialization object.
        /// </summary>
        /// <param name="plans"></param>
        internal void Import(IEnumerable<SerializablePlan> plans)
        {
            // Filter plans which belong to this owner
            List<Plan> newPlanList = plans.Where(plan => plan.Owner == m_owner.Guid).Select(
                serialPlan => new Plan(m_owner, serialPlan) { IsConnected = true }).ToList();

            // We now add the plans
            SetItems(newPlanList);

            // Fire the global event
            AppServices.TraceService?.Trace(m_owner?.Name);
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterPlanCollectionChangedEvent(m_owner));
        }
    }
}