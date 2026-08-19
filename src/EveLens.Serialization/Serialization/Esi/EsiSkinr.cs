// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EveLens.Common.Serialization.Esi
{
    /// <summary>
    /// Full SKINR design recipe from the public route
    /// <c>GET /cosmetics/skinr/{skinr_id}</c> (2026-08-18 compatibility date).
    /// Describes the design completely: nanocoating per slot and every pattern's
    /// projection and 3D transform — enough to visualize the design.
    /// </summary>
    [DataContract]
    public class EsiSkinrRecipe
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        [DataMember(Name = "name")]
        public string Name { get; set; }

        /// <summary>The SKINR line (family) this design belongs to.</summary>
        [DataMember(Name = "line")]
        public string Line { get; set; }

        [DataMember(Name = "creator_id")]
        public long CreatorId { get; set; }

        [DataMember(Name = "ship_type_id")]
        public int ShipTypeId { get; set; }

        [DataMember(Name = "tier")]
        public EsiSkinrTier Tier { get; set; }

        [DataMember(Name = "layout")]
        public EsiSkinrLayout Layout { get; set; }
    }

    [DataContract]
    public class EsiSkinrTier
    {
        [DataMember(Name = "level")]
        public int Level { get; set; }
    }

    [DataContract]
    public class EsiSkinrLayout
    {
        [DataMember(Name = "slots")]
        public List<EsiSkinrSlot> Slots { get; set; } = new();
    }

    [DataContract]
    public class EsiSkinrSlot
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "configuration")]
        public EsiSkinrSlotConfiguration Configuration { get; set; }
    }

    /// <summary>A slot holds either a nanocoating (material) or a pattern.</summary>
    [DataContract]
    public class EsiSkinrSlotConfiguration
    {
        [DataMember(Name = "nanocoating", IsRequired = false)]
        public EsiSkinrNanocoating Nanocoating { get; set; }

        [DataMember(Name = "pattern", IsRequired = false)]
        public EsiSkinrPattern Pattern { get; set; }
    }

    /// <summary>
    /// A material component. The id resolves through the SDE's
    /// skinrComponents to a localized name and a SpaceObjectFactory
    /// material resource (<c>res:/.../*.red</c>).
    /// </summary>
    [DataContract]
    public class EsiSkinrNanocoating
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }
    }

    [DataContract]
    public class EsiSkinrPattern
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }

        [DataMember(Name = "configuration")]
        public EsiSkinrPatternConfiguration Configuration { get; set; }
    }

    [DataContract]
    public class EsiSkinrPatternConfiguration
    {
        /// <summary>Which material slots the pattern projects onto.</summary>
        [DataMember(Name = "projection")]
        public EsiSkinrProjection Projection { get; set; }

        [DataMember(Name = "transform")]
        public EsiSkinrTransform Transform { get; set; }

        [DataMember(Name = "mirrored")]
        public bool Mirrored { get; set; }
    }

    [DataContract]
    public class EsiSkinrProjection
    {
        [DataMember(Name = "slot1")]
        public bool Slot1 { get; set; }

        [DataMember(Name = "slot2")]
        public bool Slot2 { get; set; }

        [DataMember(Name = "slot3")]
        public bool Slot3 { get; set; }

        [DataMember(Name = "slot4")]
        public bool Slot4 { get; set; }
    }

    /// <summary>Where a pattern sits on the hull: position, quaternion, scale.</summary>
    [DataContract]
    public class EsiSkinrTransform
    {
        [DataMember(Name = "position")]
        public EsiSkinrVector Position { get; set; }

        [DataMember(Name = "rotation")]
        public EsiSkinrQuaternion Rotation { get; set; }

        [DataMember(Name = "scaling")]
        public EsiSkinrVector Scaling { get; set; }
    }

    [DataContract]
    public class EsiSkinrVector
    {
        [DataMember(Name = "x")]
        public double X { get; set; }

        [DataMember(Name = "y")]
        public double Y { get; set; }

        [DataMember(Name = "z")]
        public double Z { get; set; }
    }

    [DataContract]
    public class EsiSkinrQuaternion
    {
        [DataMember(Name = "x")]
        public double X { get; set; }

        [DataMember(Name = "y")]
        public double Y { get; set; }

        [DataMember(Name = "z")]
        public double Z { get; set; }

        [DataMember(Name = "w")]
        public double W { get; set; }
    }

    /// <summary>
    /// One page of Paragon Hub listings from <c>GET /paragon-hub/skinr</c> and the
    /// targeted/own-listing variants. Cursor-paginated; sold/expired listings linger
    /// with their final state, which is what makes price history possible.
    /// </summary>
    [DataContract]
    public class EsiSkinrListingsPage
    {
        [DataMember(Name = "cursor")]
        public EsiSkinrCursor Cursor { get; set; }

        [DataMember(Name = "listings")]
        public List<EsiSkinrListing> Listings { get; set; } = new();
    }

    [DataContract]
    public class EsiSkinrCursor
    {
        [DataMember(Name = "before")]
        public string Before { get; set; }

        [DataMember(Name = "after")]
        public string After { get; set; }
    }

    [DataContract]
    public class EsiSkinrListing
    {
        [DataMember(Name = "id")]
        public string Id { get; set; }

        /// <summary>listed, removed, sold, expired — final states linger.</summary>
        [DataMember(Name = "state")]
        public string State { get; set; }

        [DataMember(Name = "last_modified")]
        public string LastModified { get; set; }

        [DataMember(Name = "seller_id")]
        public long SellerId { get; set; }

        [DataMember(Name = "skinr_id")]
        public string SkinrId { get; set; }

        [DataMember(Name = "created")]
        public string Created { get; set; }

        [DataMember(Name = "expires")]
        public string Expires { get; set; }

        [DataMember(Name = "quantity")]
        public int Quantity { get; set; }

        [DataMember(Name = "price")]
        public EsiSkinrPrice Price { get; set; }
    }

    [DataContract]
    public class EsiSkinrPrice
    {
        [DataMember(Name = "plex")]
        public long Plex { get; set; }
    }

    /// <summary>
    /// A character's SKINR licenses from
    /// <c>GET /characters/{character_id}/cosmetics/skinr</c> (esi.cosmetic.char:read).
    /// </summary>
    [DataContract]
    public class EsiSkinrInventory
    {
        [DataMember(Name = "licenses")]
        public List<EsiSkinrLicense> Licenses { get; set; } = new();
    }

    [DataContract]
    public class EsiSkinrLicense
    {
        [DataMember(Name = "skinr_id")]
        public string SkinrId { get; set; }

        [DataMember(Name = "activated")]
        public bool Activated { get; set; }

        [DataMember(Name = "unactivated")]
        public long Unactivated { get; set; }
    }

    /// <summary>
    /// A character's SKINR component licenses from
    /// <c>GET /characters/{character_id}/cosmetics/skinr/components</c>.
    /// </summary>
    [DataContract]
    public class EsiSkinrComponentInventory
    {
        [DataMember(Name = "licenses")]
        public List<EsiSkinrComponentLicense> Licenses { get; set; } = new();
    }

    [DataContract]
    public class EsiSkinrComponentLicense
    {
        /// <summary>Resolves through the SDE's skinrComponents file.</summary>
        [DataMember(Name = "component_id")]
        public long ComponentId { get; set; }

        /// <summary>"nanocoating" or "pattern".</summary>
        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "runs")]
        public EsiSkinrComponentRuns Runs { get; set; }
    }

    /// <summary>Either a remaining count or unlimited — ESI sends one of the two.</summary>
    [DataContract]
    public class EsiSkinrComponentRuns
    {
        [DataMember(Name = "remaining", IsRequired = false)]
        public long? Remaining { get; set; }

        [DataMember(Name = "unlimited", IsRequired = false)]
        public bool? Unlimited { get; set; }
    }
}
