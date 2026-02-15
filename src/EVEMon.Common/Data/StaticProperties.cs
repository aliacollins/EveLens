using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Collections.Global;
using EVEMon.Common.Constants;
using EVEMon.Common.Serialization.Datafiles;

namespace EVEMon.Common.Data
{
    public static class StaticProperties
    {
        #region Fields

        private static readonly Dictionary<int, EvePropertyCategory> s_categoriesByID =
            new Dictionary<int, EvePropertyCategory>();

        private static readonly Dictionary<string, EvePropertyCategory> s_categoriesByName =
            new Dictionary<string, EvePropertyCategory>();

        private static readonly Dictionary<int, EveProperty> s_propertiesByID = new Dictionary<int, EveProperty>();
        private static readonly Dictionary<string, EveProperty> s_propertiesByName = new Dictionary<string, EveProperty>();

        #endregion


        #region Initilization

        /// <summary>
        /// Initialize static properties.
        /// </summary>
        public static void Load()
        {
            PropertiesDatafile datafile = 
                Util.DeserializeDatafile<PropertiesDatafile>(DatafileConstants.PropertiesDatafile,
                    Util.LoadXslt(Properties.Resources.DatafilesXSLT));

            // Fetch deserialized data
            foreach (EvePropertyCategory category in datafile.Categories.Select(
                srcCategory => new EvePropertyCategory(srcCategory)))
            {
                s_categoriesByID[category.ID] = category;
                s_categoriesByName[category.Name] = category;

                // Store properties
                foreach (EveProperty property in category)
                {
                    s_propertiesByID[property.ID] = property;
                    s_propertiesByName[property.Name] = property;
                }
            }

            // Set visibility in ships browser
            foreach (int propertyID in DBConstants.AlwaysVisibleForShipPropertyIDs.Where(
                propertyID => s_propertiesByID.ContainsKey(propertyID)))
            {
                s_propertiesByID[propertyID].AlwaysVisibleForShips = true;
            }

            // Set hide if default for properties
            // we want to hide in browser if they just show their default value
            foreach (int propertyID in DBConstants.HideIfDefaultPropertyIDs.Where(
                propertyID => s_propertiesByID.ContainsKey(propertyID)))
            {
                s_propertiesByID[propertyID].HideIfDefault = true;
            }

            GlobalDatafileCollection.OnDatafileLoaded();
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets the list of properties categories.
        /// </summary>
        public static IEnumerable<EvePropertyCategory> AllCategories
        {
            get
            {
                if (s_categoriesByID.Keys.Any(id => id == 0))
                    return s_categoriesByName.Values;
                return s_categoriesByID.Values;
            }
        }

        /// <summary>
        /// Gets the list of properties.
        /// </summary>
        public static IEnumerable<EveProperty> AllProperties => s_propertiesByID.Values;

        #endregion


        #region Public Finders

        /// <summary>
        /// Gets a property by its id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static EveProperty GetPropertyByID(int id)
        {
            EveProperty property;
            s_propertiesByID.TryGetValue(id, out property);
            return property;
        }

        /// <summary>
        /// Gets a property by its name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static EveProperty GetPropertyByName(string name)
        {
            EveProperty property;
            s_propertiesByName.TryGetValue(name, out property);
            return property;
        }

        /// <summary>
        /// Gets a group by its id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static EvePropertyCategory GetCategoryByID(int id)
        {
            EvePropertyCategory category;
            s_categoriesByID.TryGetValue(id, out category);
            return category;
        }

        #endregion
    }
}