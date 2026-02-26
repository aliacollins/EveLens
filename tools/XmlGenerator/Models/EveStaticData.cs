namespace EveLens.XmlGenerator.Models
{
    using Microsoft.EntityFrameworkCore;
    using System.Configuration;

    public partial class EveStaticData : DbContext
    {
        private static string _connectionString;

        public EveStaticData()
        {
        }

        public EveStaticData(DbContextOptions<EveStaticData> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Read connection string from App.config
                if (string.IsNullOrEmpty(_connectionString))
                {
                    var connectionStringSetting = ConfigurationManager.ConnectionStrings["EveStaticData"];
                    if (connectionStringSetting != null)
                    {
                        _connectionString = connectionStringSetting.ConnectionString;
                    }
                    else
                    {
                        _connectionString = "Data Source=..\\..\\..\\..\\sqlite-latest.sqlite";
                    }
                }
                optionsBuilder.UseSqlite(_connectionString);
            }
        }

        public virtual DbSet<agtAgents> agtAgents { get; set; }
        public virtual DbSet<agtAgentTypes> agtAgentTypes { get; set; }
        public virtual DbSet<agtResearchAgents> agtResearchAgents { get; set; }
        public virtual DbSet<chrAncestries> chrAncestries { get; set; }
        public virtual DbSet<chrAttributes> chrAttributes { get; set; }
        public virtual DbSet<chrBloodlines> chrBloodlines { get; set; }
        public virtual DbSet<chrFactions> chrFactions { get; set; }
        public virtual DbSet<chrRaces> chrRaces { get; set; }
        public virtual DbSet<crpActivities> crpActivities { get; set; }
        public virtual DbSet<crpNPCCorporationDivisions> crpNPCCorporationDivisions { get; set; }
        public virtual DbSet<crpNPCCorporationResearchFields> crpNPCCorporationResearchFields { get; set; }
        public virtual DbSet<crpNPCCorporations> crpNPCCorporations { get; set; }
        public virtual DbSet<crpNPCCorporationTrades> crpNPCCorporationTrades { get; set; }
        public virtual DbSet<crpNPCDivisions> crpNPCDivisions { get; set; }
        public virtual DbSet<dgmAttributeCategories> dgmAttributeCategories { get; set; }
        public virtual DbSet<dgmAttributeTypes> dgmAttributeTypes { get; set; }
        public virtual DbSet<dgmEffects> dgmEffects { get; set; }
        public virtual DbSet<dgmExpressions> dgmExpressions { get; set; }
        public virtual DbSet<dgmTypeAttributes> dgmTypeAttributes { get; set; }
        public virtual DbSet<dgmTypeEffects> dgmTypeEffects { get; set; }
        public virtual DbSet<eveGraphics> eveGraphics { get; set; }
        public virtual DbSet<eveIcons> eveIcons { get; set; }
        public virtual DbSet<eveUnits> eveUnits { get; set; }
        public virtual DbSet<industryActivity> industryActivity { get; set; }
        public virtual DbSet<industryActivityMaterials> industryActivityMaterials { get; set; }
        public virtual DbSet<industryActivityProbabilities> industryActivityProbabilities { get; set; }
        public virtual DbSet<industryActivityProducts> industryActivityProducts { get; set; }
        public virtual DbSet<industryActivitySkills> industryActivitySkills { get; set; }
        public virtual DbSet<industryBlueprints> industryBlueprints { get; set; }
        public virtual DbSet<invCategories> invCategories { get; set; }
        public virtual DbSet<invContrabandTypes> invContrabandTypes { get; set; }
        public virtual DbSet<invControlTowerResourcePurposes> invControlTowerResourcePurposes { get; set; }
        public virtual DbSet<invControlTowerResources> invControlTowerResources { get; set; }
        public virtual DbSet<invFlags> invFlags { get; set; }
        public virtual DbSet<invGroups> invGroups { get; set; }
        public virtual DbSet<invItems> invItems { get; set; }
        public virtual DbSet<invMarketGroups> invMarketGroups { get; set; }
        public virtual DbSet<invMetaGroups> invMetaGroups { get; set; }
        public virtual DbSet<invMetaTypes> invMetaTypes { get; set; }
        public virtual DbSet<invNames> invNames { get; set; }
        public virtual DbSet<invPositions> invPositions { get; set; }
        public virtual DbSet<invTraits> invTraits { get; set; }
        public virtual DbSet<invTypeMaterials> invTypeMaterials { get; set; }
        public virtual DbSet<invTypeReactions> invTypeReactions { get; set; }
        public virtual DbSet<invTypes> invTypes { get; set; }
        public virtual DbSet<invUniqueNames> invUniqueNames { get; set; }
        public virtual DbSet<mapCelestialStatistics> mapCelestialStatistics { get; set; }
        public virtual DbSet<mapConstellationJumps> mapConstellationJumps { get; set; }
        public virtual DbSet<mapConstellations> mapConstellations { get; set; }
        public virtual DbSet<mapDenormalize> mapDenormalize { get; set; }
        public virtual DbSet<mapJumps> mapJumps { get; set; }
        public virtual DbSet<mapLandmarks> mapLandmarks { get; set; }
        public virtual DbSet<mapLocationScenes> mapLocationScenes { get; set; }
        public virtual DbSet<mapLocationWormholeClasses> mapLocationWormholeClasses { get; set; }
        public virtual DbSet<mapRegionJumps> mapRegionJumps { get; set; }
        public virtual DbSet<mapRegions> mapRegions { get; set; }
        public virtual DbSet<mapSolarSystemJumps> mapSolarSystemJumps { get; set; }
        public virtual DbSet<mapSolarSystems> mapSolarSystems { get; set; }
        public virtual DbSet<mapUniverse> mapUniverse { get; set; }
        public virtual DbSet<planetSchematics> planetSchematics { get; set; }
        public virtual DbSet<planetSchematicsPinMap> planetSchematicsPinMap { get; set; }
        public virtual DbSet<planetSchematicsTypeMap> planetSchematicsTypeMap { get; set; }
        public virtual DbSet<ramActivities> ramActivities { get; set; }
        public virtual DbSet<ramAssemblyLineStations> ramAssemblyLineStations { get; set; }
        public virtual DbSet<ramAssemblyLineTypeDetailPerCategory> ramAssemblyLineTypeDetailPerCategory { get; set; }
        public virtual DbSet<ramAssemblyLineTypeDetailPerGroup> ramAssemblyLineTypeDetailPerGroup { get; set; }
        public virtual DbSet<ramAssemblyLineTypes> ramAssemblyLineTypes { get; set; }
        public virtual DbSet<ramInstallationTypeContents> ramInstallationTypeContents { get; set; }
        public virtual DbSet<sknLicenses> sknLicenses { get; set; }
        public virtual DbSet<sknMaterials> sknMaterials { get; set; }
        public virtual DbSet<sknSkins> sknSkins { get; set; }
        public virtual DbSet<staOperations> staOperations { get; set; }
        public virtual DbSet<staOperationServices> staOperationServices { get; set; }
        public virtual DbSet<staServices> staServices { get; set; }
        public virtual DbSet<staStations> staStations { get; set; }
        public virtual DbSet<staStationTypes> staStationTypes { get; set; }
        public virtual DbSet<translationTables> translationTables { get; set; }
        public virtual DbSet<trnTranslationColumns> trnTranslationColumns { get; set; }
        public virtual DbSet<trnTranslationLanguages> trnTranslationLanguages { get; set; }
        public virtual DbSet<trnTranslations> trnTranslations { get; set; }
        public virtual DbSet<warCombatZones> warCombatZones { get; set; }
        public virtual DbSet<warCombatZoneSystems> warCombatZoneSystems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure entity mappings for tables without explicit primary keys
            // EF Core requires primary keys, so we define them here

            modelBuilder.Entity<agtAgents>().HasKey(e => e.agentID);
            modelBuilder.Entity<agtAgentTypes>().HasKey(e => e.agentTypeID);
            modelBuilder.Entity<agtResearchAgents>().HasKey(e => new { e.agentID, e.typeID });
            modelBuilder.Entity<chrAncestries>().HasKey(e => e.ancestryID);
            modelBuilder.Entity<chrAttributes>().HasKey(e => e.attributeID);
            modelBuilder.Entity<chrBloodlines>().HasKey(e => e.bloodlineID);
            modelBuilder.Entity<chrFactions>().HasKey(e => e.factionID);
            modelBuilder.Entity<chrRaces>().HasKey(e => e.raceID);
            modelBuilder.Entity<crpActivities>().HasKey(e => e.activityID);
            modelBuilder.Entity<crpNPCCorporationDivisions>().HasKey(e => new { e.corporationID, e.divisionID });
            modelBuilder.Entity<crpNPCCorporationResearchFields>().HasKey(e => new { e.skillID, e.corporationID });
            modelBuilder.Entity<crpNPCCorporations>().HasKey(e => e.corporationID);
            modelBuilder.Entity<crpNPCCorporationTrades>().HasKey(e => new { e.corporationID, e.typeID });
            modelBuilder.Entity<crpNPCDivisions>().HasKey(e => e.divisionID);
            modelBuilder.Entity<dgmAttributeCategories>().HasKey(e => e.categoryID);
            modelBuilder.Entity<dgmAttributeTypes>().HasKey(e => e.attributeID);
            modelBuilder.Entity<dgmEffects>().HasKey(e => e.effectID);
            modelBuilder.Entity<dgmExpressions>().HasKey(e => e.expressionID);
            modelBuilder.Entity<dgmTypeAttributes>().HasKey(e => new { e.typeID, e.attributeID });
            modelBuilder.Entity<dgmTypeEffects>().HasKey(e => new { e.typeID, e.effectID });
            modelBuilder.Entity<eveGraphics>().HasKey(e => e.graphicID);
            modelBuilder.Entity<eveIcons>().HasKey(e => e.iconID);
            modelBuilder.Entity<eveUnits>().HasKey(e => e.unitID);
            modelBuilder.Entity<industryActivity>().HasKey(e => new { e.typeID, e.activityID });
            modelBuilder.Entity<industryActivityMaterials>().HasKey(e => new { e.typeID, e.activityID, e.materialTypeID });
            modelBuilder.Entity<industryActivityProbabilities>().HasKey(e => new { e.typeID, e.activityID, e.productTypeID });
            modelBuilder.Entity<industryActivityProducts>().HasKey(e => new { e.typeID, e.activityID, e.productTypeID });
            modelBuilder.Entity<industryActivitySkills>().HasKey(e => new { e.typeID, e.activityID, e.skillID });
            modelBuilder.Entity<industryBlueprints>().HasKey(e => e.typeID);
            modelBuilder.Entity<invCategories>().HasKey(e => e.categoryID);
            modelBuilder.Entity<invContrabandTypes>().HasKey(e => new { e.factionID, e.typeID });
            modelBuilder.Entity<invControlTowerResourcePurposes>().HasKey(e => e.purpose);
            modelBuilder.Entity<invControlTowerResources>().HasKey(e => new { e.controlTowerTypeID, e.resourceTypeID });
            modelBuilder.Entity<invFlags>().HasKey(e => e.flagID);
            modelBuilder.Entity<invGroups>().HasKey(e => e.groupID);
            modelBuilder.Entity<invItems>().HasKey(e => e.itemID);
            modelBuilder.Entity<invMarketGroups>().HasKey(e => e.marketGroupID);
            modelBuilder.Entity<invMetaGroups>().HasKey(e => e.metaGroupID);
            modelBuilder.Entity<invMetaTypes>().HasKey(e => e.typeID);
            modelBuilder.Entity<invNames>().HasKey(e => e.itemID);
            modelBuilder.Entity<invPositions>().HasKey(e => e.itemID);
            modelBuilder.Entity<invTraits>().HasKey(e => e.traitID);
            modelBuilder.Entity<invTypeMaterials>().HasKey(e => new { e.typeID, e.materialTypeID });
            modelBuilder.Entity<invTypeReactions>().HasKey(e => new { e.reactionTypeID, e.input, e.typeID });
            modelBuilder.Entity<invTypes>().HasKey(e => e.typeID);
            modelBuilder.Entity<invUniqueNames>().HasKey(e => e.itemID);
            modelBuilder.Entity<mapCelestialStatistics>().HasKey(e => e.celestialID);
            modelBuilder.Entity<mapConstellationJumps>().HasKey(e => new { e.fromConstellationID, e.toConstellationID });
            modelBuilder.Entity<mapConstellations>().HasKey(e => e.constellationID);
            modelBuilder.Entity<mapDenormalize>().HasKey(e => e.itemID);
            modelBuilder.Entity<mapJumps>().HasKey(e => e.stargateID);
            modelBuilder.Entity<mapLandmarks>().HasKey(e => e.landmarkID);
            modelBuilder.Entity<mapLocationScenes>().HasKey(e => e.locationID);
            modelBuilder.Entity<mapLocationWormholeClasses>().HasKey(e => e.locationID);
            modelBuilder.Entity<mapRegionJumps>().HasKey(e => new { e.fromRegionID, e.toRegionID });
            modelBuilder.Entity<mapRegions>().HasKey(e => e.regionID);
            modelBuilder.Entity<mapSolarSystemJumps>().HasKey(e => new { e.fromSolarSystemID, e.toSolarSystemID });
            modelBuilder.Entity<mapSolarSystems>().HasKey(e => e.solarSystemID);
            modelBuilder.Entity<mapUniverse>().HasKey(e => e.universeID);
            modelBuilder.Entity<planetSchematics>().HasKey(e => e.schematicID);
            modelBuilder.Entity<planetSchematicsPinMap>().HasKey(e => new { e.schematicID, e.pinTypeID });
            modelBuilder.Entity<planetSchematicsTypeMap>().HasKey(e => new { e.schematicID, e.typeID });
            modelBuilder.Entity<ramActivities>().HasKey(e => e.activityID);
            modelBuilder.Entity<ramAssemblyLineStations>().HasKey(e => new { e.stationID, e.assemblyLineTypeID });
            modelBuilder.Entity<ramAssemblyLineTypeDetailPerCategory>().HasKey(e => new { e.assemblyLineTypeID, e.categoryID });
            modelBuilder.Entity<ramAssemblyLineTypeDetailPerGroup>().HasKey(e => new { e.assemblyLineTypeID, e.groupID });
            modelBuilder.Entity<ramAssemblyLineTypes>().HasKey(e => e.assemblyLineTypeID);
            modelBuilder.Entity<ramInstallationTypeContents>().HasKey(e => new { e.installationTypeID, e.assemblyLineTypeID });
            modelBuilder.Entity<sknLicenses>().HasKey(e => e.licenseTypeID);
            modelBuilder.Entity<sknMaterials>().HasKey(e => e.skinMaterialID);
            modelBuilder.Entity<sknSkins>().HasKey(e => e.skinID);
            modelBuilder.Entity<staOperations>().HasKey(e => e.operationID);
            modelBuilder.Entity<staOperationServices>().HasKey(e => new { e.operationID, e.serviceID });
            modelBuilder.Entity<staServices>().HasKey(e => e.serviceID);
            modelBuilder.Entity<staStations>().HasKey(e => e.stationID);
            modelBuilder.Entity<staStationTypes>().HasKey(e => e.stationTypeID);
            modelBuilder.Entity<translationTables>().HasKey(e => new { e.sourceTable, e.translatedKey });
            modelBuilder.Entity<trnTranslationColumns>().HasKey(e => e.tcID);
            modelBuilder.Entity<trnTranslationLanguages>().HasKey(e => e.numericLanguageID);
            modelBuilder.Entity<trnTranslations>().HasKey(e => new { e.tcID, e.keyID, e.languageID });
            modelBuilder.Entity<warCombatZones>().HasKey(e => e.combatZoneID);
            modelBuilder.Entity<warCombatZoneSystems>().HasKey(e => e.solarSystemID);
        }
    }
}
