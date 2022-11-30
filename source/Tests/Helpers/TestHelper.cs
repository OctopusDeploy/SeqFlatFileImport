using System.IO;
using NUnit.Framework;
using SeqFlatFileImport.Core;

namespace Tests.Helpers
{
    public static class TestHelper
    {
        public static string GetFilePath(string inputFileName)
        {
            return Path.Combine(
                Path.GetDirectoryName(typeof(TestHelper).Assembly.Location) ?? throw new DirectoryNotFoundException(),
                "LogFiles",
                inputFileName
                );
        }

        public static void ShouldBeSuccessful(this IResult result)
        {
            if (result.WasFailure)
                Assert.Fail(result.ErrorString);
        }
    }
}