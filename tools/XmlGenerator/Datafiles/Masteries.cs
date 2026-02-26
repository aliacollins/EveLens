using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EveLens.Common.Collections;
using EveLens.Common.Serialization.Datafiles;
using EveLens.XmlGenerator.Providers;
using EveLens.XmlGenerator.StaticData;
using EveLens.XmlGenerator.Utils;

namespace EveLens.XmlGenerator.Datafiles
{
    internal static class Masteries
    {
        /// <summary>
        /// Generate the masteries datafile.
        /// </summary>        
        internal static void GenerateDatafile()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Util.ResetCounters();

            Console.WriteLine();
            Console.Write(@"Generating masteries datafile... ");

            // Export the mastery ships
            List<SerializableMasteryShip> listOfMasteryShips = new List<SerializableMasteryShip>();

            foreach (IGrouping<int, DgmTypeMasteries> typeMastery in Database.DgmTypeMasteriesTable.GroupBy(x=> x.ItemID))
            {
                SerializableMasteryShip masteryShip = new SerializableMasteryShip
                {
                    ID = typeMastery.Key,
                    Name = Database.InvTypesTable[typeMastery.Key].Name
                };

                // Add masteries to mastery ship
                masteryShip.Masteries.AddRange(ExportMasteries(typeMastery).OrderBy(x => x.Grade));

                // Add mastery ship
                listOfMasteryShips.Add(masteryShip);
            }

            // Serialize
            MasteriesDatafile datafile = new MasteriesDatafile();
            datafile.MasteryShips.AddRange(listOfMasteryShips);

            Util.DisplayEndTime(stopwatch);

            Util.SerializeXml(datafile, DatafileConstants.MasteriesDatafile);
        }

        /// <summary>
        /// Exports the masteries.
        /// </summary>
        /// <param name="typeMasteries">The type masteries.</param>
        /// <returns></returns>
        private static IEnumerable<SerializableMastery> ExportMasteries(IGrouping<int, DgmTypeMasteries> typeMasteries)
        {
            List<SerializableMastery> listOfMasteries = new List<SerializableMastery>();

            foreach (DgmMasteries typeMastery in typeMasteries.Select(x => Database.DgmMasteriesTable[x.MasteryID]))
            {
                Util.UpdatePercentDone(Database.MasteriesTotalCount);

                int grade = typeMastery.Grade + 1;

                SerializableMastery mastery;
                if (listOfMasteries.All(x=> x.Grade != grade))
                {
                    mastery = new SerializableMastery { Grade = grade };
                    listOfMasteries.Add(mastery);
                }
                else
                    mastery = listOfMasteries.First(x => x.Grade == grade);

                SerializableMasteryCertificate masteryCertificate = new SerializableMasteryCertificate
                {
                    ID = typeMastery.CertificateID,
                    ClassName =
                        Database.CrtClassesTable[Database.CrtCertificatesTable[typeMastery.CertificateID].ClassID].ClassName
                };

                mastery.Certificates.Add(masteryCertificate);
            }

            return listOfMasteries;
        }
    }
}