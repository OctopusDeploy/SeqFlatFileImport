using System.IO;
using NUnit.Framework;
using SeqFlatFileImport.Tests.Helpers;

namespace SeqFlatFileImport.Tests
{
    public class AutodetectByFileNameTests
    {
        [TestCase("OctopusServer.txt", TestName = "Octopus Server", ExpectedResult = "OctopusServer")]
        [TestCase("u_ex160412.log", TestName = "IIS Log", ExpectedResult = "IIS")]
        [TestCase("Web-2016-05-29.log", TestName = "Octopus Web Requests", ExpectedResult = "OctopusWebLog")]
        [TestCase("ServerTasks-16572.log.txt", TestName = "Octopus Raw Task", ExpectedResult = "OctopusRawTask")]
        [TestCase("servertasks-21_gjzwyyv2kt.txt", TestName = "Octopus Task", ExpectedResult = "OctopusTask")]
        public string Execute(string inputFileName)
        {
            var result = new Importer(new StubSeqEndpoint())
                .AutoDetectFormat(new MemoryStream(), inputFileName);

            result.ShouldBeSuccessful();

            return result.Value.Name;
        }
    }
}