using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using PowerPlannerAppDataLibrary.Importers;
using PowerPlannerAppDataLibrary.DataLayer.DataItems;

namespace PowerPlannerAppDataLibraryUnitTests
{
    [TestClass]
    public class CunyImporterTests
    {
        [TestMethod]
        public void ImportsSingleClassWithMeetings()
        {
            var sample = @"
            [
              {
                ""Name"": ""CSI 135: Discrete Structures"",
                ""ShortName"": ""CSI 135"",
                ""Section"": ""LEC 01"",
                ""Instructor"": ""J. Doe"",
                ""Credits"": 3,
                ""ColorHex"": ""#5B8DEF"",
                ""TermStart"": ""2025-08-28"",
                ""TermEnd"": ""2025-12-20"",
                ""Meetings"": [
                  { ""Days"": [""Mon"", ""Wed""], ""Start"": ""09:30"", ""End"": ""10:45"", ""Location"": ""Carman 234"" }
                ]
              }
            ]";

            var result = CunyImporter.ImportFromCunyJson(sample).ToList();
            Assert.AreEqual(1, result.Count);

            var cls = result[0];
            Assert.AreEqual("CSI 135: Discrete Structures", cls.Name);
            Assert.AreEqual("CSI 135", cls.ShortName);
            Assert.AreEqual(3f, cls.Credits);

            // Verify a schedule child exists
            var sched = cls.Children.OfType<DataItemSchedule>().FirstOrDefault();
            Assert.IsNotNull(sched, "Schedule should be created");
            Assert.IsTrue(sched.StartTime.TotalMinutes > 0);
        }
    }
}
