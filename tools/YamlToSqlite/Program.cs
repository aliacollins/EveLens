using System.Globalization;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace EveLens.YamlToSqlite;

/// <summary>
/// Converts EVE Online SDE YAML files directly to SQLite database.
/// Replaces the need for EVESDEToSQL which requires SQL Server.
/// </summary>
class Program
{
    private static SqliteConnection? _connection;
    private static IDeserializer? _yamlDeserializer;
    private static string _yamlPath = "";
    private static int _totalTables;
    private static int _currentTable;

    // HttpClient for ESI requests (station names not in YAML SDE)
    private static readonly HttpClient _httpClient = new HttpClient
    {
        DefaultRequestHeaders =
        {
            { "User-Agent", "EveLens-SDE-Generator/1.0 (github.com/aliacollins/evelens; contact: eve:Alia Collins)" }
        }
    };

    static void Main(string[] args)
    {
        Console.WriteLine("===========================================");
        Console.WriteLine(" EVE SDE YAML to SQLite Converter");
        Console.WriteLine(" For EveLens - .NET 8 Version");
        Console.WriteLine("===========================================");
        Console.WriteLine();

        // Determine paths
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        _yamlPath = args.Length > 0 ? args[0] : Path.Combine(baseDir, "..", "..", "..", "..", "SDEFiles", "yaml_extracted");
        string outputPath = args.Length > 1 ? args[1] : Path.Combine(baseDir, "..", "..", "..", "..", "sqlite-latest.sqlite");

        // Resolve to absolute paths
        _yamlPath = Path.GetFullPath(_yamlPath);
        outputPath = Path.GetFullPath(outputPath);

        Console.WriteLine($"YAML Source: {_yamlPath}");
        Console.WriteLine($"Output DB:   {outputPath}");
        Console.WriteLine();

        if (!Directory.Exists(_yamlPath))
        {
            Console.WriteLine($"ERROR: YAML directory not found: {_yamlPath}");
            Console.WriteLine("Please extract the SDE YAML files first.");
            return;
        }

        // Initialize YAML deserializer
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        // Delete existing database
        if (File.Exists(outputPath))
        {
            Console.WriteLine("Removing existing database...");
            File.Delete(outputPath);
        }

        // Create and open connection
        var connectionString = $"Data Source={outputPath}";
        _connection = new SqliteConnection(connectionString);
        _connection.Open();

        Console.WriteLine("Creating SQLite database...");
        Console.WriteLine();

        var startTime = DateTime.Now;
        _totalTables = 37; // 36 imports + 1 ESI fetch
        _currentTable = 0;

        try
        {
            // Create all tables and import data
            ImportAgentTypes();
            ImportIcons();
            ImportUnits();
            ImportAttributeCategories();
            ImportAttributeTypes();
            ImportCategories();
            ImportGroups();
            ImportMarketGroups();
            ImportTypes();
            ImportTypeDogma();
            ImportFactions();
            ImportNpcDivisions();
            ImportBlueprints();
            ImportTypeMaterials();
            ImportControlTowerResources();
            ImportRegions();
            ImportConstellations();
            ImportSolarSystems();
            ImportSolarSystemJumps();
            ImportStations();
            FetchStationNamesFromEsi().GetAwaiter().GetResult();
            ImportAgents();
            ImportResearchAgents();
            ImportNpcCorporations();
            ImportItems();
            ImportNames();
            ImportFlags();
            ImportMetaTypes();
            ImportTypeReactions();
            ImportTraits();
            ImportControlTowerResourcePurposes();

            Console.WriteLine();
            Console.WriteLine($"Completed in {DateTime.Now.Subtract(startTime):g}");
            Console.WriteLine($"Database saved to: {outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        finally
        {
            _connection?.Close();
        }
    }

    private static void UpdateProgress(string tableName)
    {
        _currentTable++;
        int percent = (_currentTable * 100) / _totalTables;
        Console.WriteLine($"[{percent,3}%] {tableName}");
    }

    private static Dictionary<TKey, TValue> LoadYaml<TKey, TValue>(string fileName) where TKey : notnull
    {
        var filePath = Path.Combine(_yamlPath, fileName);
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"  Warning: {fileName} not found, skipping...");
            return new Dictionary<TKey, TValue>();
        }

        using var reader = new StreamReader(filePath);
        return _yamlDeserializer!.Deserialize<Dictionary<TKey, TValue>>(reader) ?? new Dictionary<TKey, TValue>();
    }

    private static List<T> LoadYamlList<T>(string fileName)
    {
        var filePath = Path.Combine(_yamlPath, fileName);
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"  Warning: {fileName} not found, skipping...");
            return new List<T>();
        }

        using var reader = new StreamReader(filePath);
        return _yamlDeserializer!.Deserialize<List<T>>(reader) ?? new List<T>();
    }

    private static void ExecuteNonQuery(string sql)
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static string GetLocalizedName(Dictionary<string, string>? names, string defaultValue = "")
    {
        if (names == null) return defaultValue;
        if (names.TryGetValue("en", out var en)) return en;
        return names.Values.FirstOrDefault() ?? defaultValue;
    }

    private static string Escape(string? value) => value?.Replace("'", "''") ?? "";

    #region Import Methods

    private static void ImportAgentTypes()
    {
        UpdateProgress("agtAgentTypes");
        ExecuteNonQuery(@"CREATE TABLE agtAgentTypes (
            agentTypeID INTEGER PRIMARY KEY,
            agentType TEXT
        )");

        var data = LoadYaml<int, AgentTypeData>("agentTypes.yaml");
        foreach (var kvp in data)
        {
            ExecuteNonQuery($"INSERT INTO agtAgentTypes VALUES ({kvp.Key}, '{Escape(kvp.Value.AgentType)}')");
        }
    }

    private static void ImportIcons()
    {
        UpdateProgress("eveIcons");
        ExecuteNonQuery(@"CREATE TABLE eveIcons (
            iconID INTEGER PRIMARY KEY,
            iconFile TEXT
        )");

        var data = LoadYaml<int, IconData>("icons.yaml");
        foreach (var kvp in data)
        {
            ExecuteNonQuery($"INSERT INTO eveIcons VALUES ({kvp.Key}, '{Escape(kvp.Value.IconFile)}')");
        }
    }

    private static void ImportUnits()
    {
        UpdateProgress("eveUnits");
        ExecuteNonQuery(@"CREATE TABLE eveUnits (
            unitID INTEGER PRIMARY KEY,
            unitName TEXT,
            displayName TEXT,
            description TEXT
        )");

        var data = LoadYaml<int, UnitData>("dogmaUnits.yaml");
        foreach (var kvp in data)
        {
            var v = kvp.Value;
            var displayName = GetLocalizedName(v.DisplayName);
            var description = GetLocalizedName(v.Description);
            ExecuteNonQuery($"INSERT INTO eveUnits VALUES ({kvp.Key}, '{Escape(v.Name)}', '{Escape(displayName)}', '{Escape(description)}')");
        }
    }

    private static void ImportAttributeCategories()
    {
        UpdateProgress("dgmAttributeCategories");
        ExecuteNonQuery(@"CREATE TABLE dgmAttributeCategories (
            categoryID INTEGER PRIMARY KEY,
            categoryName TEXT,
            categoryDescription TEXT
        )");

        var data = LoadYaml<int, AttributeCategoryData>("dogmaAttributeCategories.yaml");
        foreach (var kvp in data)
        {
            var v = kvp.Value;
            ExecuteNonQuery($"INSERT INTO dgmAttributeCategories VALUES ({kvp.Key}, '{Escape(v.Name)}', '{Escape(v.Description)}')");
        }
    }

    private static void ImportAttributeTypes()
    {
        UpdateProgress("dgmAttributeTypes");
        ExecuteNonQuery(@"CREATE TABLE dgmAttributeTypes (
            attributeID INTEGER PRIMARY KEY,
            attributeName TEXT,
            description TEXT,
            iconID INTEGER,
            defaultValue REAL,
            published INTEGER,
            displayName TEXT,
            unitID INTEGER,
            stackable INTEGER,
            highIsGood INTEGER,
            categoryID INTEGER
        )");

        var data = LoadYaml<int, AttributeTypeData>("dogmaAttributes.yaml");
        foreach (var kvp in data)
        {
            var v = kvp.Value;
            var displayName = GetLocalizedName(v.DisplayName);
            ExecuteNonQuery($@"INSERT INTO dgmAttributeTypes VALUES (
                {kvp.Key}, '{Escape(v.Name)}', '{Escape(v.Description)}', {v.IconID?.ToString() ?? "NULL"},
                {v.DefaultValue?.ToString(CultureInfo.InvariantCulture) ?? "NULL"}, {(v.Published == true ? 1 : 0)},
                '{Escape(displayName)}', {v.UnitID?.ToString() ?? "NULL"}, {(v.Stackable == true ? 1 : 0)},
                {(v.HighIsGood == true ? 1 : 0)}, {v.AttributeCategoryID?.ToString() ?? "NULL"})");
        }
    }

    private static void ImportCategories()
    {
        UpdateProgress("invCategories");
        ExecuteNonQuery(@"CREATE TABLE invCategories (
            categoryID INTEGER PRIMARY KEY,
            categoryName TEXT,
            iconID INTEGER,
            published INTEGER
        )");

        var data = LoadYaml<int, CategoryData>("categories.yaml");
        foreach (var kvp in data)
        {
            var v = kvp.Value;
            var name = GetLocalizedName(v.Name);
            ExecuteNonQuery($"INSERT INTO invCategories VALUES ({kvp.Key}, '{Escape(name)}', {v.IconID?.ToString() ?? "NULL"}, {(v.Published == true ? 1 : 0)})");
        }
    }

    private static void ImportGroups()
    {
        UpdateProgress("invGroups");
        ExecuteNonQuery(@"CREATE TABLE invGroups (
            groupID INTEGER PRIMARY KEY,
            categoryID INTEGER,
            groupName TEXT,
            iconID INTEGER,
            useBasePrice INTEGER,
            anchored INTEGER,
            anchorable INTEGER,
            fittableNonSingleton INTEGER,
            published INTEGER
        )");

        var data = LoadYaml<int, GroupData>("groups.yaml");
        foreach (var kvp in data)
        {
            var v = kvp.Value;
            var name = GetLocalizedName(v.Name);
            ExecuteNonQuery($@"INSERT INTO invGroups VALUES (
                {kvp.Key}, {v.CategoryID?.ToString() ?? "NULL"}, '{Escape(name)}', {v.IconID?.ToString() ?? "NULL"},
                {(v.UseBasePrice == true ? 1 : 0)}, {(v.Anchored == true ? 1 : 0)}, {(v.Anchorable == true ? 1 : 0)},
                {(v.FittableNonSingleton == true ? 1 : 0)}, {(v.Published == true ? 1 : 0)})");
        }
    }

    private static void ImportMarketGroups()
    {
        UpdateProgress("invMarketGroups");
        ExecuteNonQuery(@"CREATE TABLE invMarketGroups (
            marketGroupID INTEGER PRIMARY KEY,
            parentGroupID INTEGER,
            marketGroupName TEXT,
            description TEXT,
            iconID INTEGER,
            hasTypes INTEGER
        )");

        var data = LoadYaml<int, MarketGroupData>("marketGroups.yaml");
        foreach (var kvp in data)
        {
            var v = kvp.Value;
            var name = GetLocalizedName(v.NameID);
            var desc = GetLocalizedName(v.DescriptionID);
            ExecuteNonQuery($@"INSERT INTO invMarketGroups VALUES (
                {kvp.Key}, {v.ParentGroupID?.ToString() ?? "NULL"}, '{Escape(name)}', '{Escape(desc)}',
                {v.IconID?.ToString() ?? "NULL"}, {(v.HasTypes == true ? 1 : 0)})");
        }
    }

    private static void ImportTypes()
    {
        UpdateProgress("invTypes");
        ExecuteNonQuery(@"CREATE TABLE invTypes (
            typeID INTEGER PRIMARY KEY,
            groupID INTEGER,
            typeName TEXT,
            description TEXT,
            mass REAL,
            volume REAL,
            capacity REAL,
            portionSize INTEGER,
            raceID INTEGER,
            basePrice REAL,
            published INTEGER,
            marketGroupID INTEGER,
            graphicID INTEGER,
            iconID INTEGER,
            soundID INTEGER
        )");

        var data = LoadYaml<int, TypeData>("types.yaml");
        int count = 0;
        int total = data.Count;

        using var transaction = _connection!.BeginTransaction();
        foreach (var kvp in data)
        {
            var v = kvp.Value;
            var name = GetLocalizedName(v.Name);
            var desc = GetLocalizedName(v.Description);

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO invTypes VALUES (
                @id, @groupID, @name, @desc, @mass, @volume, @capacity, @portionSize,
                @raceID, @basePrice, @published, @marketGroupID, @graphicID, @iconID, @soundID)";
            cmd.Parameters.AddWithValue("@id", kvp.Key);
            cmd.Parameters.AddWithValue("@groupID", v.GroupID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@desc", desc);
            cmd.Parameters.AddWithValue("@mass", v.Mass ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@volume", v.Volume ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@capacity", v.Capacity ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@portionSize", v.PortionSize ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@raceID", v.RaceID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@basePrice", v.BasePrice ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@published", v.Published == true ? 1 : 0);
            cmd.Parameters.AddWithValue("@marketGroupID", v.MarketGroupID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@graphicID", v.GraphicID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@iconID", v.IconID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@soundID", v.SoundID ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();

            count++;
            if (count % 10000 == 0)
                Console.WriteLine($"       {count}/{total} types...");
        }
        transaction.Commit();
    }

    private static void ImportTypeDogma()
    {
        UpdateProgress("dgmTypeAttributes");
        ExecuteNonQuery(@"CREATE TABLE dgmTypeAttributes (
            typeID INTEGER,
            attributeID INTEGER,
            valueInt INTEGER,
            valueFloat REAL,
            PRIMARY KEY (typeID, attributeID)
        )");

        UpdateProgress("dgmTypeEffects");
        ExecuteNonQuery(@"CREATE TABLE dgmTypeEffects (
            typeID INTEGER,
            effectID INTEGER,
            isDefault INTEGER,
            PRIMARY KEY (typeID, effectID)
        )");

        var data = LoadYaml<int, TypeDogmaData>("typeDogma.yaml");
        int count = 0;
        int total = data.Count;

        using var transaction = _connection!.BeginTransaction();
        foreach (var kvp in data)
        {
            int typeID = kvp.Key;
            var v = kvp.Value;

            // Import attributes
            if (v.DogmaAttributes != null)
            {
                foreach (var attr in v.DogmaAttributes)
                {
                    using var cmd = _connection.CreateCommand();
                    cmd.CommandText = "INSERT OR IGNORE INTO dgmTypeAttributes VALUES (@typeID, @attrID, @valueInt, @valueFloat)";
                    cmd.Parameters.AddWithValue("@typeID", typeID);
                    cmd.Parameters.AddWithValue("@attrID", attr.AttributeID);
                    cmd.Parameters.AddWithValue("@valueInt", attr.Value != null ? (long)attr.Value : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@valueFloat", attr.Value ?? (object)DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }

            // Import effects
            if (v.DogmaEffects != null)
            {
                foreach (var effect in v.DogmaEffects)
                {
                    using var cmd = _connection.CreateCommand();
                    cmd.CommandText = "INSERT OR IGNORE INTO dgmTypeEffects VALUES (@typeID, @effectID, @isDefault)";
                    cmd.Parameters.AddWithValue("@typeID", typeID);
                    cmd.Parameters.AddWithValue("@effectID", effect.EffectID);
                    cmd.Parameters.AddWithValue("@isDefault", effect.IsDefault == true ? 1 : 0);
                    cmd.ExecuteNonQuery();
                }
            }

            count++;
            if (count % 10000 == 0)
                Console.WriteLine($"       {count}/{total} type dogma...");
        }
        transaction.Commit();
    }

    private static void ImportFactions()
    {
        UpdateProgress("chrFactions");
        ExecuteNonQuery(@"CREATE TABLE chrFactions (
            factionID INTEGER PRIMARY KEY,
            factionName TEXT,
            description TEXT,
            raceIDs INTEGER,
            solarSystemID INTEGER,
            corporationID INTEGER,
            sizeFactor REAL,
            stationCount INTEGER,
            stationSystemCount INTEGER,
            militiaCorporationID INTEGER,
            iconID INTEGER
        )");

        var data = LoadYaml<int, FactionData>("factions.yaml");
        foreach (var kvp in data)
        {
            var v = kvp.Value;
            var name = GetLocalizedName(v.NameID);
            var desc = GetLocalizedName(v.DescriptionID);
            ExecuteNonQuery($@"INSERT INTO chrFactions VALUES (
                {kvp.Key}, '{Escape(name)}', '{Escape(desc)}', {v.RaceIDs?.ToString() ?? "NULL"},
                {v.SolarSystemID?.ToString() ?? "NULL"}, {v.CorporationID?.ToString() ?? "NULL"},
                {v.SizeFactor?.ToString(CultureInfo.InvariantCulture) ?? "NULL"},
                {v.StationCount?.ToString() ?? "NULL"}, {v.StationSystemCount?.ToString() ?? "NULL"},
                {v.MilitiaCorporationID?.ToString() ?? "NULL"}, {v.IconID?.ToString() ?? "NULL"})");
        }
    }

    private static void ImportNpcDivisions()
    {
        UpdateProgress("crpNPCDivisions");
        ExecuteNonQuery(@"CREATE TABLE crpNPCDivisions (
            divisionID INTEGER PRIMARY KEY,
            divisionName TEXT,
            description TEXT,
            leaderType TEXT
        )");

        // NPC Divisions come from npcCorporationDivisions.yaml
        var data = LoadYaml<int, NpcDivisionData>("npcCorporationDivisions.yaml");
        foreach (var kvp in data)
        {
            var v = kvp.Value;
            var name = GetLocalizedName(v.NameID);
            var desc = GetLocalizedName(v.DescriptionID);
            ExecuteNonQuery($"INSERT INTO crpNPCDivisions VALUES ({kvp.Key}, '{Escape(name)}', '{Escape(desc)}', '{Escape(v.LeaderType)}')");
        }
    }

    private static void ImportBlueprints()
    {
        UpdateProgress("industryBlueprints");
        ExecuteNonQuery(@"CREATE TABLE industryBlueprints (
            typeID INTEGER PRIMARY KEY,
            maxProductionLimit INTEGER
        )");

        UpdateProgress("industryActivity");
        ExecuteNonQuery(@"CREATE TABLE industryActivity (
            typeID INTEGER,
            activityID INTEGER,
            time INTEGER,
            PRIMARY KEY (typeID, activityID)
        )");

        UpdateProgress("industryActivityMaterials");
        ExecuteNonQuery(@"CREATE TABLE industryActivityMaterials (
            typeID INTEGER,
            activityID INTEGER,
            materialTypeID INTEGER,
            quantity INTEGER
        )");

        UpdateProgress("industryActivityProducts");
        ExecuteNonQuery(@"CREATE TABLE industryActivityProducts (
            typeID INTEGER,
            activityID INTEGER,
            productTypeID INTEGER,
            quantity INTEGER,
            probability REAL
        )");

        UpdateProgress("industryActivitySkills");
        ExecuteNonQuery(@"CREATE TABLE industryActivitySkills (
            typeID INTEGER,
            activityID INTEGER,
            skillID INTEGER,
            level INTEGER
        )");

        UpdateProgress("industryActivityProbabilities");
        ExecuteNonQuery(@"CREATE TABLE industryActivityProbabilities (
            typeID INTEGER,
            activityID INTEGER,
            productTypeID INTEGER,
            probability REAL
        )");

        var data = LoadYaml<int, BlueprintData>("blueprints.yaml");
        int count = 0;
        int total = data.Count;

        using var transaction = _connection!.BeginTransaction();
        foreach (var kvp in data)
        {
            int typeID = kvp.Key;
            var v = kvp.Value;

            // Blueprint itself
            ExecuteNonQuery($"INSERT INTO industryBlueprints VALUES ({typeID}, {v.MaxProductionLimit ?? 0})");

            // Activities
            if (v.Activities != null)
            {
                foreach (var actKvp in v.Activities)
                {
                    int activityID = GetActivityID(actKvp.Key);
                    var act = actKvp.Value;

                    ExecuteNonQuery($"INSERT INTO industryActivity VALUES ({typeID}, {activityID}, {act.Time ?? 0})");

                    // Materials
                    if (act.Materials != null)
                    {
                        foreach (var mat in act.Materials)
                        {
                            ExecuteNonQuery($"INSERT INTO industryActivityMaterials VALUES ({typeID}, {activityID}, {mat.TypeID}, {mat.Quantity})");
                        }
                    }

                    // Products
                    if (act.Products != null)
                    {
                        foreach (var prod in act.Products)
                        {
                            ExecuteNonQuery($"INSERT INTO industryActivityProducts VALUES ({typeID}, {activityID}, {prod.TypeID}, {prod.Quantity}, {prod.Probability?.ToString(CultureInfo.InvariantCulture) ?? "NULL"})");
                        }
                    }

                    // Skills
                    if (act.Skills != null)
                    {
                        foreach (var skill in act.Skills)
                        {
                            ExecuteNonQuery($"INSERT INTO industryActivitySkills VALUES ({typeID}, {activityID}, {skill.TypeID}, {skill.Level})");
                        }
                    }
                }
            }

            count++;
            if (count % 5000 == 0)
                Console.WriteLine($"       {count}/{total} blueprints...");
        }
        transaction.Commit();
    }

    private static int GetActivityID(string activityName) => activityName.ToLower() switch
    {
        "manufacturing" => 1,
        "researching_time_efficiency" or "research_time" => 3,
        "researching_material_efficiency" or "research_material" => 4,
        "copying" => 5,
        "invention" => 8,
        "reaction" => 11,
        _ => 0
    };

    private static void ImportTypeMaterials()
    {
        UpdateProgress("invTypeMaterials");
        ExecuteNonQuery(@"CREATE TABLE invTypeMaterials (
            typeID INTEGER,
            materialTypeID INTEGER,
            quantity INTEGER,
            PRIMARY KEY (typeID, materialTypeID)
        )");

        var data = LoadYaml<int, TypeMaterialsWrapper>("typeMaterials.yaml");
        using var transaction = _connection!.BeginTransaction();
        foreach (var kvp in data)
        {
            int typeID = kvp.Key;
            if (kvp.Value.Materials != null)
            {
                foreach (var mat in kvp.Value.Materials)
                {
                    ExecuteNonQuery($"INSERT OR IGNORE INTO invTypeMaterials VALUES ({typeID}, {mat.MaterialTypeID}, {mat.Quantity})");
                }
            }
        }
        transaction.Commit();
    }

    private static void ImportControlTowerResources()
    {
        UpdateProgress("invControlTowerResources");
        ExecuteNonQuery(@"CREATE TABLE invControlTowerResources (
            controlTowerTypeID INTEGER,
            resourceTypeID INTEGER,
            purpose INTEGER,
            quantity INTEGER,
            minSecurityLevel REAL,
            factionID INTEGER,
            PRIMARY KEY (controlTowerTypeID, resourceTypeID)
        )");

        var data = LoadYaml<int, ControlTowerResourcesWrapper>("controlTowerResources.yaml");
        using var transaction = _connection!.BeginTransaction();
        foreach (var kvp in data)
        {
            int towerTypeID = kvp.Key;
            if (kvp.Value.Resources != null)
            {
                foreach (var res in kvp.Value.Resources)
                {
                    ExecuteNonQuery($@"INSERT OR IGNORE INTO invControlTowerResources VALUES (
                        {towerTypeID}, {res.ResourceTypeID}, {res.Purpose ?? 0}, {res.Quantity ?? 0},
                        {res.MinSecurityLevel?.ToString(CultureInfo.InvariantCulture) ?? "NULL"},
                        {res.FactionID?.ToString() ?? "NULL"})");
                }
            }
        }
        transaction.Commit();
    }

    private static void ImportRegions()
    {
        UpdateProgress("mapRegions");
        ExecuteNonQuery(@"CREATE TABLE mapRegions (
            regionID INTEGER PRIMARY KEY,
            regionName TEXT,
            factionID INTEGER
        )");

        var data = LoadYaml<int, RegionData>("mapRegions.yaml");
        foreach (var kvp in data)
        {
            var v = kvp.Value;
            var name = GetLocalizedName(v.NameID);
            ExecuteNonQuery($"INSERT INTO mapRegions VALUES ({kvp.Key}, '{Escape(name)}', {v.FactionID?.ToString() ?? "NULL"})");
        }
    }

    private static void ImportConstellations()
    {
        UpdateProgress("mapConstellations");
        ExecuteNonQuery(@"CREATE TABLE mapConstellations (
            constellationID INTEGER PRIMARY KEY,
            constellationName TEXT,
            regionID INTEGER,
            x REAL,
            y REAL,
            z REAL,
            xMin REAL,
            xMax REAL,
            yMin REAL,
            yMax REAL,
            zMin REAL,
            zMax REAL,
            factionID INTEGER,
            radius REAL
        )");

        var data = LoadYaml<int, ConstellationData>("mapConstellations.yaml");
        foreach (var kvp in data)
        {
            var v = kvp.Value;
            var name = GetLocalizedName(v.NameID);
            using var cmd = _connection!.CreateCommand();
            cmd.CommandText = @"INSERT INTO mapConstellations VALUES (
                @id, @name, @regionID, @x, @y, @z, @xMin, @xMax, @yMin, @yMax, @zMin, @zMax, @factionID, @radius)";
            cmd.Parameters.AddWithValue("@id", kvp.Key);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@regionID", v.RegionID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@x", v.Center?.X ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@y", v.Center?.Y ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@z", v.Center?.Z ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@xMin", v.Min?.X ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@xMax", v.Max?.X ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@yMin", v.Min?.Y ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@yMax", v.Max?.Y ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@zMin", v.Min?.Z ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@zMax", v.Max?.Z ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@factionID", v.FactionID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@radius", v.Radius ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }
    }

    private static void ImportSolarSystems()
    {
        UpdateProgress("mapSolarSystems");
        ExecuteNonQuery(@"CREATE TABLE mapSolarSystems (
            solarSystemID INTEGER PRIMARY KEY,
            solarSystemName TEXT,
            regionID INTEGER,
            constellationID INTEGER,
            x REAL, y REAL, z REAL,
            xMin REAL, xMax REAL, yMin REAL, yMax REAL, zMin REAL, zMax REAL,
            luminosity REAL, border INTEGER, fringe INTEGER, corridor INTEGER, hub INTEGER,
            international INTEGER, regional INTEGER, constellation INTEGER,
            security REAL, factionID INTEGER, radius REAL, sunTypeID INTEGER, securityClass TEXT
        )");

        var data = LoadYaml<int, SolarSystemData>("mapSolarSystems.yaml");
        using var transaction = _connection!.BeginTransaction();
        foreach (var kvp in data)
        {
            var v = kvp.Value;
            var name = GetLocalizedName(v.NameID);
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO mapSolarSystems VALUES (
                @id, @name, @regionID, @constID, @x, @y, @z,
                @xMin, @xMax, @yMin, @yMax, @zMin, @zMax,
                @luminosity, @border, @fringe, @corridor, @hub,
                @international, @regional, @constellation,
                @security, @factionID, @radius, @sunTypeID, @secClass)";
            cmd.Parameters.AddWithValue("@id", kvp.Key);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@regionID", v.RegionID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@constID", v.ConstellationID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@x", v.Center?.X ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@y", v.Center?.Y ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@z", v.Center?.Z ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@xMin", v.Min?.X ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@xMax", v.Max?.X ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@yMin", v.Min?.Y ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@yMax", v.Max?.Y ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@zMin", v.Min?.Z ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@zMax", v.Max?.Z ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@luminosity", v.Luminosity ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@border", v.Border == true ? 1 : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@fringe", v.Fringe == true ? 1 : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@corridor", v.Corridor == true ? 1 : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@hub", v.Hub == true ? 1 : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@international", v.International == true ? 1 : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@regional", v.Regional == true ? 1 : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@constellation", v.Constellation == true ? 1 : (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@security", v.Security ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@factionID", v.FactionID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@radius", v.Radius ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@sunTypeID", v.SunTypeID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@secClass", v.SecurityClass ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    private static void ImportSolarSystemJumps()
    {
        UpdateProgress("mapSolarSystemJumps");
        ExecuteNonQuery(@"CREATE TABLE mapSolarSystemJumps (
            fromSolarSystemID INTEGER,
            toSolarSystemID INTEGER,
            PRIMARY KEY (fromSolarSystemID, toSolarSystemID)
        )");

        // Jumps are derived from stargates
        var stargates = LoadYaml<int, StargateData>("mapStargates.yaml");
        using var transaction = _connection!.BeginTransaction();
        foreach (var kvp in stargates)
        {
            var v = kvp.Value;
            if (v.SolarSystemID.HasValue && v.Destination?.SolarSystemID != null)
            {
                ExecuteNonQuery($"INSERT OR IGNORE INTO mapSolarSystemJumps VALUES ({v.SolarSystemID}, {v.Destination.SolarSystemID})");
            }
        }
        transaction.Commit();
    }

    private static void ImportStations()
    {
        UpdateProgress("staStations");
        ExecuteNonQuery(@"CREATE TABLE staStations (
            stationID INTEGER PRIMARY KEY,
            security REAL,
            dockingCostPerVolume REAL,
            maxShipVolumeDockable REAL,
            officeRentalCost INTEGER,
            operationID INTEGER,
            stationTypeID INTEGER,
            corporationID INTEGER,
            solarSystemID INTEGER,
            constellationID INTEGER,
            regionID INTEGER,
            stationName TEXT,
            x REAL,
            y REAL,
            z REAL,
            reprocessingEfficiency REAL,
            reprocessingStationsTake REAL,
            reprocessingHangarFlag INTEGER
        )");

        var data = LoadYaml<int, StationData>("npcStations.yaml");
        using var transaction = _connection!.BeginTransaction();
        foreach (var kvp in data)
        {
            var v = kvp.Value;
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO staStations VALUES (
                @id, @security, @dockCost, @maxShipVol, @officeRent, @opID, @typeID, @corpID,
                @solarSystemID, @constID, @regionID, @name, @x, @y, @z, @reEff, @reTake, @reFlag)";
            cmd.Parameters.AddWithValue("@id", kvp.Key);
            cmd.Parameters.AddWithValue("@security", v.Security ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@dockCost", v.DockingCostPerVolume ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@maxShipVol", v.MaxShipVolumeDockable ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@officeRent", v.OfficeRentalCost ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@opID", v.OperationID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@typeID", v.TypeID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@corpID", v.OwnerID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@solarSystemID", v.SolarSystemID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@constID", v.ConstellationID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@regionID", v.RegionID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@name", v.StationName ?? "");
            cmd.Parameters.AddWithValue("@x", v.X ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@y", v.Y ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@z", v.Z ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@reEff", v.ReprocessingEfficiency ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@reTake", v.ReprocessingStationsTake ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@reFlag", v.ReprocessingHangarFlag ?? (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    /// <summary>
    /// Fetches station names from ESI for all NPC stations.
    /// The YAML SDE doesn't include station names - they must be fetched from ESI.
    /// This is run during data generation, not at runtime.
    /// </summary>
    private static async Task FetchStationNamesFromEsi()
    {
        UpdateProgress("Fetching station names from ESI");

        // Get all station IDs that need names
        var stationIds = new List<int>();
        using (var cmd = _connection!.CreateCommand())
        {
            cmd.CommandText = "SELECT stationID FROM staStations WHERE stationName IS NULL OR stationName = ''";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                stationIds.Add(reader.GetInt32(0));
            }
        }

        if (stationIds.Count == 0)
        {
            Console.WriteLine("       All stations already have names.");
            return;
        }

        Console.WriteLine($"       Fetching {stationIds.Count} station names from ESI...");

        int fetched = 0;
        int failed = 0;
        var updateCmd = _connection.CreateCommand();
        updateCmd.CommandText = "UPDATE staStations SET stationName = @name WHERE stationID = @id";
        var nameParam = updateCmd.Parameters.Add("@name", SqliteType.Text);
        var idParam = updateCmd.Parameters.Add("@id", SqliteType.Integer);

        using var transaction = _connection.BeginTransaction();
        updateCmd.Transaction = transaction;

        foreach (var stationId in stationIds)
        {
            try
            {
                var url = $"https://esi.evetech.net/latest/universe/stations/{stationId}/?datasource=tranquility";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("name", out var nameProp))
                    {
                        var name = nameProp.GetString() ?? "";
                        nameParam.Value = name;
                        idParam.Value = stationId;
                        updateCmd.ExecuteNonQuery();
                        fetched++;
                    }
                }
                else
                {
                    failed++;
                }

                // Progress every 100 stations
                if ((fetched + failed) % 100 == 0)
                {
                    Console.WriteLine($"       {fetched + failed}/{stationIds.Count} processed ({fetched} ok, {failed} failed)");
                }

                // Small delay to avoid rate limiting (ESI allows 20 req/s, we'll do 10/s to be safe)
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"       Warning: Failed to fetch station {stationId}: {ex.Message}");
                failed++;
            }
        }

        transaction.Commit();
        Console.WriteLine($"       Done: {fetched} names fetched, {failed} failed.");
    }

    private static void ImportAgents()
    {
        UpdateProgress("agtAgents");
        ExecuteNonQuery(@"CREATE TABLE agtAgents (
            agentID INTEGER PRIMARY KEY,
            divisionID INTEGER,
            corporationID INTEGER,
            locationID INTEGER,
            level INTEGER,
            quality INTEGER,
            agentTypeID INTEGER,
            isLocator INTEGER
        )");

        // Agents come from npcCharacters.yaml
        var data = LoadYaml<int, NpcCharacterData>("npcCharacters.yaml");
        using var transaction = _connection!.BeginTransaction();
        foreach (var kvp in data)
        {
            var v = kvp.Value;
            if (v.Agent != null)
            {
                var a = v.Agent;
                ExecuteNonQuery($@"INSERT INTO agtAgents VALUES (
                    {kvp.Key}, {a.DivisionID?.ToString() ?? "NULL"}, {v.CorporationID?.ToString() ?? "NULL"},
                    {a.LocationID?.ToString() ?? "NULL"}, {a.Level?.ToString() ?? "NULL"}, {a.Quality?.ToString() ?? "0"},
                    {a.AgentTypeID?.ToString() ?? "NULL"}, {(a.IsLocator == true ? 1 : 0)})");
            }
        }
        transaction.Commit();
    }

    private static void ImportResearchAgents()
    {
        UpdateProgress("agtResearchAgents");
        ExecuteNonQuery(@"CREATE TABLE agtResearchAgents (
            agentID INTEGER,
            typeID INTEGER,
            PRIMARY KEY (agentID, typeID)
        )");

        var data = LoadYaml<int, NpcCharacterData>("npcCharacters.yaml");
        using var transaction = _connection!.BeginTransaction();
        foreach (var kvp in data)
        {
            var v = kvp.Value;
            if (v.Agent?.ResearchSkillIDs != null)
            {
                foreach (var skillID in v.Agent.ResearchSkillIDs)
                {
                    ExecuteNonQuery($"INSERT OR IGNORE INTO agtResearchAgents VALUES ({kvp.Key}, {skillID})");
                }
            }
        }
        transaction.Commit();
    }

    private static void ImportNpcCorporations()
    {
        UpdateProgress("crpNPCCorporations");
        ExecuteNonQuery(@"CREATE TABLE crpNPCCorporations (
            corporationID INTEGER PRIMARY KEY,
            corporationName TEXT,
            size TEXT,
            extent TEXT,
            solarSystemID INTEGER,
            stationID INTEGER,
            factionID INTEGER,
            description TEXT
        )");

        var data = LoadYaml<int, NpcCorporationData>("npcCorporations.yaml");
        using var transaction = _connection!.BeginTransaction();
        foreach (var kvp in data)
        {
            var v = kvp.Value;
            var name = GetLocalizedName(v.NameID);
            var desc = GetLocalizedName(v.DescriptionID);
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"INSERT INTO crpNPCCorporations VALUES (
                @id, @name, @size, @extent, @solarSystemID, @stationID, @factionID, @desc)";
            cmd.Parameters.AddWithValue("@id", kvp.Key);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@size", v.Size ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@extent", v.Extent ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@solarSystemID", v.SolarSystemID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@stationID", v.StationID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@factionID", v.FactionID ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@desc", desc);
            cmd.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    private static void ImportItems()
    {
        UpdateProgress("invItems");
        ExecuteNonQuery(@"CREATE TABLE invItems (
            itemID INTEGER PRIMARY KEY,
            typeID INTEGER,
            ownerID INTEGER,
            locationID INTEGER,
            flagID INTEGER,
            quantity INTEGER
        )");
        // Items are not directly in YAML SDE - they come from universe data
        // This table may be populated differently or left mostly empty
    }

    private static void ImportNames()
    {
        UpdateProgress("invNames");
        ExecuteNonQuery(@"CREATE TABLE invNames (
            itemID INTEGER PRIMARY KEY,
            itemName TEXT
        )");
        // Names come from various sources - regions, constellations, solar systems, stations, corporations
        // We'll populate from what we have

        using var transaction = _connection!.BeginTransaction();

        // Add region names
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = "INSERT OR IGNORE INTO invNames SELECT regionID, regionName FROM mapRegions";
            cmd.ExecuteNonQuery();
        }

        // Add constellation names
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = "INSERT OR IGNORE INTO invNames SELECT constellationID, constellationName FROM mapConstellations";
            cmd.ExecuteNonQuery();
        }

        // Add solar system names
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = "INSERT OR IGNORE INTO invNames SELECT solarSystemID, solarSystemName FROM mapSolarSystems";
            cmd.ExecuteNonQuery();
        }

        // Add station names
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = "INSERT OR IGNORE INTO invNames SELECT stationID, stationName FROM staStations";
            cmd.ExecuteNonQuery();
        }

        // Add NPC corporation names
        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = "INSERT OR IGNORE INTO invNames SELECT corporationID, corporationName FROM crpNPCCorporations";
            cmd.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private static void ImportFlags()
    {
        UpdateProgress("invFlags");
        ExecuteNonQuery(@"CREATE TABLE invFlags (
            flagID INTEGER PRIMARY KEY,
            flagName TEXT,
            flagText TEXT,
            orderID INTEGER
        )");
        // Flags are hardcoded in EVE - we'll add common ones
        var flags = new[] {
            (0, "None", "None", 0),
            (5, "Cargo", "Cargo", 5),
            (87, "DroneBay", "Drone Bay", 87),
            (89, "Implant", "Implant", 89),
            (90, "ShipHangar", "Ship Hangar", 90),
        };
        foreach (var (id, name, text, order) in flags)
        {
            ExecuteNonQuery($"INSERT INTO invFlags VALUES ({id}, '{name}', '{text}', {order})");
        }
    }

    private static void ImportMetaTypes()
    {
        UpdateProgress("invMetaTypes");
        ExecuteNonQuery(@"CREATE TABLE invMetaTypes (
            typeID INTEGER PRIMARY KEY,
            parentTypeID INTEGER,
            metaGroupID INTEGER
        )");
        // Meta types link items to their base versions
        // This info can be derived from type attributes (metaGroupID attribute)
    }

    private static void ImportTypeReactions()
    {
        UpdateProgress("invTypeReactions");
        ExecuteNonQuery(@"CREATE TABLE invTypeReactions (
            reactionTypeID INTEGER,
            input INTEGER,
            typeID INTEGER,
            quantity INTEGER,
            PRIMARY KEY (reactionTypeID, input, typeID)
        )");
        // Reactions come from blueprints with reaction activity
    }

    private static void ImportTraits()
    {
        UpdateProgress("invTraits");
        ExecuteNonQuery(@"CREATE TABLE invTraits (
            traitID INTEGER PRIMARY KEY,
            typeID INTEGER,
            skillID INTEGER,
            bonus REAL,
            BonusText TEXT,
            unitID INTEGER
        )");

        var data = LoadYaml<int, TypeBonusData>("typeBonus.yaml");
        int traitID = 1;
        using var transaction = _connection!.BeginTransaction();
        foreach (var kvp in data)
        {
            int typeID = kvp.Key;
            var v = kvp.Value;

            // Role bonuses (no skill required)
            if (v.RoleBonuses != null)
            {
                foreach (var bonus in v.RoleBonuses)
                {
                    var text = GetLocalizedName(bonus.BonusText);
                    ExecuteNonQuery($@"INSERT INTO invTraits VALUES (
                        {traitID++}, {typeID}, NULL, {bonus.Bonus?.ToString(CultureInfo.InvariantCulture) ?? "NULL"},
                        '{Escape(text)}', {bonus.UnitID?.ToString() ?? "NULL"})");
                }
            }

            // Skill bonuses
            if (v.TypeBonuses != null)
            {
                foreach (var bonusKvp in v.TypeBonuses)
                {
                    int skillID = bonusKvp.Key;
                    foreach (var bonus in bonusKvp.Value)
                    {
                        var text = GetLocalizedName(bonus.BonusText);
                        ExecuteNonQuery($@"INSERT INTO invTraits VALUES (
                            {traitID++}, {typeID}, {skillID}, {bonus.Bonus?.ToString(CultureInfo.InvariantCulture) ?? "NULL"},
                            '{Escape(text)}', {bonus.UnitID?.ToString() ?? "NULL"})");
                    }
                }
            }
        }
        transaction.Commit();
    }

    private static void ImportControlTowerResourcePurposes()
    {
        UpdateProgress("invControlTowerResourcePurposes");
        ExecuteNonQuery(@"CREATE TABLE invControlTowerResourcePurposes (
            purpose INTEGER PRIMARY KEY,
            purposeText TEXT
        )");
        // Hardcoded purposes
        var purposes = new[] {
            (1, "Online"),
            (2, "Power"),
            (3, "CPU"),
            (4, "Reinforced"),
            (5, "Offline"),
        };
        foreach (var (id, text) in purposes)
        {
            ExecuteNonQuery($"INSERT INTO invControlTowerResourcePurposes VALUES ({id}, '{text}')");
        }
    }

    #endregion
}

#region YAML Data Classes

class AgentTypeData { public string? AgentType { get; set; } }
class IconData { public string? IconFile { get; set; } }
class UnitData { public string? Name { get; set; } public Dictionary<string, string>? DisplayName { get; set; } public Dictionary<string, string>? Description { get; set; } }
class AttributeCategoryData { public string? Name { get; set; } public string? Description { get; set; } }
class AttributeTypeData {
    public string? Name { get; set; } public string? Description { get; set; } public Dictionary<string, string>? DisplayName { get; set; }
    public int? IconID { get; set; } public double? DefaultValue { get; set; } public bool? Published { get; set; }
    public int? UnitID { get; set; } public bool? Stackable { get; set; } public bool? HighIsGood { get; set; } public int? AttributeCategoryID { get; set; }
}
class CategoryData { public Dictionary<string, string>? Name { get; set; } public int? IconID { get; set; } public bool? Published { get; set; } }
class GroupData {
    public Dictionary<string, string>? Name { get; set; } public int? CategoryID { get; set; } public int? IconID { get; set; }
    public bool? UseBasePrice { get; set; } public bool? Anchored { get; set; } public bool? Anchorable { get; set; }
    public bool? FittableNonSingleton { get; set; } public bool? Published { get; set; }
}
class MarketGroupData {
    [YamlMember(Alias = "name")]
    public Dictionary<string, string>? NameID { get; set; }
    [YamlMember(Alias = "description")]
    public Dictionary<string, string>? DescriptionID { get; set; }
    public int? ParentGroupID { get; set; } public int? IconID { get; set; } public bool? HasTypes { get; set; }
}
class TypeData {
    public Dictionary<string, string>? Name { get; set; } public Dictionary<string, string>? Description { get; set; }
    public int? GroupID { get; set; } public double? Mass { get; set; } public double? Volume { get; set; }
    public double? Capacity { get; set; } public int? PortionSize { get; set; } public int? RaceID { get; set; }
    public double? BasePrice { get; set; } public bool? Published { get; set; } public int? MarketGroupID { get; set; }
    public int? GraphicID { get; set; } public int? IconID { get; set; } public int? SoundID { get; set; }
}
class TypeDogmaData { public List<DogmaAttributeEntry>? DogmaAttributes { get; set; } public List<DogmaEffectEntry>? DogmaEffects { get; set; } }
class DogmaAttributeEntry { public int AttributeID { get; set; } public double? Value { get; set; } }
class DogmaEffectEntry { public int EffectID { get; set; } public bool? IsDefault { get; set; } }
class FactionData {
    public Dictionary<string, string>? NameID { get; set; } public Dictionary<string, string>? DescriptionID { get; set; }
    public int? RaceIDs { get; set; } public int? SolarSystemID { get; set; } public int? CorporationID { get; set; }
    public double? SizeFactor { get; set; } public int? StationCount { get; set; } public int? StationSystemCount { get; set; }
    public int? MilitiaCorporationID { get; set; } public int? IconID { get; set; }
}
class NpcDivisionData { public Dictionary<string, string>? NameID { get; set; } public Dictionary<string, string>? DescriptionID { get; set; } public string? LeaderType { get; set; } }
class BlueprintData { public int? MaxProductionLimit { get; set; } public Dictionary<string, BlueprintActivityData>? Activities { get; set; } }
class BlueprintActivityData {
    public int? Time { get; set; } public List<BlueprintMaterial>? Materials { get; set; }
    public List<BlueprintProduct>? Products { get; set; } public List<BlueprintSkill>? Skills { get; set; }
}
class BlueprintMaterial { public int TypeID { get; set; } public int Quantity { get; set; } }
class BlueprintProduct { public int TypeID { get; set; } public int Quantity { get; set; } public double? Probability { get; set; } }
class BlueprintSkill { public int TypeID { get; set; } public int Level { get; set; } }
class TypeMaterialsWrapper { public List<TypeMaterialEntry>? Materials { get; set; } }
class TypeMaterialEntry { public int MaterialTypeID { get; set; } public int Quantity { get; set; } }
class ControlTowerResourcesWrapper { public List<ControlTowerResourceEntry>? Resources { get; set; } }
class ControlTowerResourceEntry { public int ResourceTypeID { get; set; } public int? Purpose { get; set; } public int? Quantity { get; set; } public double? MinSecurityLevel { get; set; } public int? FactionID { get; set; } }
class RegionData { [YamlMember(Alias = "name")] public Dictionary<string, string>? NameID { get; set; } public int? FactionID { get; set; } }
class ConstellationData {
    [YamlMember(Alias = "name")]
    public Dictionary<string, string>? NameID { get; set; } public int? RegionID { get; set; }
    [YamlMember(Alias = "position")]
    public CoordinateData? Center { get; set; } public CoordinateData? Min { get; set; } public CoordinateData? Max { get; set; }
    public int? FactionID { get; set; } public double? Radius { get; set; }
}
class SolarSystemData {
    [YamlMember(Alias = "name")]
    public Dictionary<string, string>? NameID { get; set; } public int? ConstellationID { get; set; } public int? RegionID { get; set; }
    [YamlMember(Alias = "securityStatus")]
    public double? Security { get; set; } public string? SecurityClass { get; set; }
    [YamlMember(Alias = "position")]
    public CoordinateData? Center { get; set; } public CoordinateData? Min { get; set; } public CoordinateData? Max { get; set; }
    public double? Luminosity { get; set; } public bool? Border { get; set; } public bool? Fringe { get; set; }
    public bool? Corridor { get; set; } public bool? Hub { get; set; } public bool? International { get; set; }
    public bool? Regional { get; set; } public bool? Constellation { get; set; }
    public int? FactionID { get; set; } public double? Radius { get; set; } public int? SunTypeID { get; set; }
}
class CoordinateData { public double? X { get; set; } public double? Y { get; set; } public double? Z { get; set; } }
class StargateData { public int? SolarSystemID { get; set; } public StargateDestination? Destination { get; set; } }
class StargateDestination { public int? SolarSystemID { get; set; } }
class StationData {
    public string? StationName { get; set; } public int? SolarSystemID { get; set; } public int? ConstellationID { get; set; }
    public int? RegionID { get; set; } public int? OwnerID { get; set; } public int? TypeID { get; set; }
    public double? Security { get; set; } public double? ReprocessingEfficiency { get; set; } public double? ReprocessingStationsTake { get; set; }
    public double? DockingCostPerVolume { get; set; } public double? MaxShipVolumeDockable { get; set; }
    public int? OfficeRentalCost { get; set; } public int? OperationID { get; set; }
    public double? X { get; set; } public double? Y { get; set; } public double? Z { get; set; }
    public int? ReprocessingHangarFlag { get; set; }
}
class NpcCharacterData { public int? CorporationID { get; set; } public AgentData? Agent { get; set; } }
class NpcCorporationData {
    [YamlMember(Alias = "name")]
    public Dictionary<string, string>? NameID { get; set; }
    [YamlMember(Alias = "description")]
    public Dictionary<string, string>? DescriptionID { get; set; }
    public string? Size { get; set; }
    public string? Extent { get; set; }
    public int? SolarSystemID { get; set; }
    public int? StationID { get; set; }
    public int? FactionID { get; set; }
}
class AgentData {
    public int? DivisionID { get; set; } public int? LocationID { get; set; } public int? Level { get; set; }
    public int? Quality { get; set; } public int? AgentTypeID { get; set; } public bool? IsLocator { get; set; }
    public List<int>? ResearchSkillIDs { get; set; }
}
class TypeBonusData { public List<BonusEntry>? RoleBonuses { get; set; } public Dictionary<int, List<BonusEntry>>? TypeBonuses { get; set; } }
class BonusEntry { public double? Bonus { get; set; } public Dictionary<string, string>? BonusText { get; set; } public int? UnitID { get; set; } }

#endregion
