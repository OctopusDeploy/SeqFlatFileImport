using System.IO;
using FluentAssertions;
using NUnit.Framework;
using SeqFlatFileImport.Tests.Helpers;

namespace SeqFlatFileImport.Tests
{
    public class AutodetectByContentsTest
    {

        [TestCase("u_ex160412.log", TestName = "IIS Log", ExpectedResult = "IIS")]
        [TestCase("Web-2016-05-29.log", TestName = "Octopus Web Requests", ExpectedResult = "OctopusWebLog")]
        public string Execute(string inputFileName)
        {
            using (var fs = File.OpenRead(TestHelper.GetFilePath(inputFileName)))
            {
                var result = new Importer(new StubSeqEndpoint()).AutoDetectFormat(fs, "");

                result.ShouldBeSuccessful();

                return result.Value.Name;
            }
        }
    }
}
