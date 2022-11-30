using System.Runtime.CompilerServices;
using Assent;
using NUnit.Framework;
using SeqFlatFileImport.Core;
using Tests.Helpers;

namespace Tests.Parse
{
    public class ParseTests
    {
        [Test]
        public void OctopusServer()
        {
            Execute("OctopusServer.txt");
        }

        [Test]
        public void OctopusWebLog()
        {
            Execute("Web-2017-03-28.log");
        }


        [Test]
        public void Iis()
        {
            Execute("u_ex150708.log");
        }

        [Test]
        public void OctopusTask()
        {
            Execute("ServerTasks-16572.log.txt");
        }

        [Test]
        public void OctopusRawTask()
        {
            Execute("servertasks-21_gjzwyyv2kt.txt");
        }

        private void Execute(string inputFileName, [CallerMemberName] string testName = null)
        {
            var seqServer = new StubSeqEndpoint();
            var result = new Importer(seqServer)
                .Import(TestHelper.GetFilePath(inputFileName));
            result.ShouldBeSuccessful();
            this.Assent(
                seqServer.LogsAsJson,
                new Configuration().UsingExtension("json"),
                testName: testName
            );
        }
    }
}