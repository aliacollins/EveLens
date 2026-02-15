using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EVEMon.Common.Collections.Global;
using EVEMon.Common.Serialization.Datafiles;
using EVEMon.Core;

namespace EVEMon.Common.Data
{
    /// <summary>
    /// The static list of the certificates.
    /// </summary>
    public static class StaticCertificates
    {
        private static readonly Dictionary<string, StaticCertificateClass> s_classesByName =
            new Dictionary<string, StaticCertificateClass>();

        private static readonly Dictionary<int, StaticCertificate> s_certificatesByID = new Dictionary<int, StaticCertificate>();


        #region Initialization

        /// <summary>
        /// Initialize static certificates.
        /// </summary>
        public static void Load()
        {
            CertificatesDatafile datafile = Util.DeserializeDatafile<CertificatesDatafile>(DatafileConstants.CertificatesDatafile,
                    Util.LoadXslt(ServiceLocator.ResourceProvider.DatafilesXSLT));

            AllGroups = new Collection<StaticCertificateGroup>();

            foreach (SerializableCertificateGroup srcGroup in datafile.Groups)
            {
                AllGroups.Add(new StaticCertificateGroup(srcGroup));
            }

            // Build inner collections
            foreach (StaticCertificateClass certClass in AllGroups.SelectMany(certClass => certClass))
            {
                s_classesByName[certClass.Name] = certClass;
                s_certificatesByID[certClass.Certificate.ID] = certClass.Certificate;
            }

            // Completes intialization
            foreach (SerializableCertificateClass srcClass in datafile.Groups.SelectMany(srcGroup => srcGroup.Classes))
            {
                s_classesByName[srcClass.Name].Certificate.CompleteInitialization(srcClass.Certificate.Prerequisites);
            }

            GlobalDatafileCollection.OnDatafileLoaded();
        }

        #endregion


        #region Public Properties

        /// <summary>
        /// Gets the categories, sorted by name.
        /// </summary>
        public static Collection<StaticCertificateGroup> AllGroups { get; private set; }

        /// <summary>
        /// Gets the certificate classes, hierarchically sorted (category's name, class's name).
        /// </summary>
        public static IEnumerable<StaticCertificateClass> AllClasses => AllGroups.SelectMany(certClass => certClass);

        /// <summary>
        /// Gets the certificates, hierarchically sorted (category's name, class's name, grade).
        /// </summary>
        public static IEnumerable<StaticCertificate> AllCertificates
            => AllGroups
                .SelectMany(group => group, (group, certClass) => new { group, certClass })
                .Select(x => x.certClass.Certificate);

        #endregion


        #region Public Finders

        /// <summary>
        /// Gets the certificate with the specified ID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static StaticCertificate GetCertificateByID(int id) => s_certificatesByID[id];

        /// <summary>
        /// Gets the certificates class with the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static StaticCertificateClass GetCertificateClassByName(string name) => s_classesByName[name];

        #endregion
    }
}