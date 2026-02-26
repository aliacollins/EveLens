using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using EveLens.Common.Collections;
using EveLens.Common.Enumerations;
using EveLens.Common.Serialization.Datafiles;
using EveLens.XmlGenerator.Interfaces;
using EveLens.XmlGenerator.Providers;
using EveLens.XmlGenerator.StaticData;
using EveLens.XmlGenerator.Utils;

namespace EveLens.XmlGenerator.Datafiles
{
    internal static class Certificates
    {
        /// <summary>
        /// Generate the certificates datafile.
        /// </summary>        
        internal static void GenerateDatafile()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Util.ResetCounters();

            Console.WriteLine();
            Console.Write(@"Generating certificates datafile... ");

            // Export certificates groups
            List<SerializableCertificateGroup> listOfCertGroups = new List<SerializableCertificateGroup>();

            foreach (InvGroups group in Database.CrtCertificatesTable.GroupBy(x => x.GroupID)
                .Select(x => Database.InvGroupsTable[x.Key]).OrderBy(x => x.Name))
            {
                SerializableCertificateGroup crtGroup = new SerializableCertificateGroup
                {
                    ID = group.ID,
                    Name = group.Name,
                    Description = group.Description
                };

                // Add classes to categories
                crtGroup.Classes.AddRange(ExportCertificateClasses(group).OrderBy(x => x.Name));

                //// Add category
                listOfCertGroups.Add(crtGroup);
            }

            // Serialize
            CertificatesDatafile datafile = new CertificatesDatafile();
            datafile.Groups.AddRange(listOfCertGroups);

            Util.DisplayEndTime(stopwatch);

            Util.SerializeXml(datafile, DatafileConstants.CertificatesDatafile);
        }

        /// <summary>
        /// Exports the certificates classes.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns></returns>
        private static IEnumerable<SerializableCertificateClass> ExportCertificateClasses(IHasID group)
        {
            List<SerializableCertificateClass> listOfCertClasses = new List<SerializableCertificateClass>();

            // Exclude unused classes
            foreach (CrtClasses certClass in Database.CrtClassesTable)
            {
                Util.UpdatePercentDone(Database.CertificatesTotalCount);

                SerializableCertificateClass crtClass = new SerializableCertificateClass
                {
                    ID = certClass.ID,
                    Name = certClass.ClassName,
                    Description = certClass.Description
                };

                // Export certificate
                SerializableCertificate certificate = Database.CrtCertificatesTable
                    .Where(x => x.ClassID == certClass.ID && x.GroupID == group.ID).Select(ExportCertificate).FirstOrDefault();

                if (certificate == null)
                    continue;

                // Add certificate to class
                crtClass.Certificate = certificate;

                // Add certificate class to classes
                listOfCertClasses.Add(crtClass);
            }
            return listOfCertClasses;
        }

        /// <summary>
        /// Exports the certificate.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        private static SerializableCertificate ExportCertificate(CrtCertificates certificate)
        {

            SerializableCertificate crtCertificate = new SerializableCertificate
            {
                ID = certificate.ID,
                Description = certificate.Description
            };

            // Export prerequesities
            IEnumerable<SerializableCertificatePrerequisite> listOfPrereq = Database.CrtRelationshipsTable
                .Where(x => x.ChildID == certificate.ID && x.ParentLevel != 0)
                .Select(relationship => new SerializableCertificatePrerequisite
                {
                    ID = Database.InvTypesTable[relationship.ParentTypeID].ID,
                    Skill = Database.InvTypesTable[relationship.ParentTypeID].Name,
                    Level = relationship.ParentLevel.ToString(CultureInfo.InvariantCulture),
                    Grade = (CertificateGrade)Enum.ToObject(typeof(CertificateGrade), relationship.Grade),
                });

            //Add prerequisites to certificate
            crtCertificate.Prerequisites.AddRange(listOfPrereq.OrderBy(x => x.Grade));

            // Add recommendations to certificate
            IEnumerable<SerializableCertificateRecommendation> listOfRecommendations = Database.CrtRecommendationsTable.Where(
                x => x.CertificateID == certificate.ID)
                .Select(recommendation => new SerializableCertificateRecommendation
                {
                    ID = recommendation.ShipTypeID,
                    ShipName = Database.InvTypesTable[recommendation.ShipTypeID].Name,
                });

            crtCertificate.Recommendations.AddRange(listOfRecommendations);

            // Add certificate
            return crtCertificate;
        }
    }
}
