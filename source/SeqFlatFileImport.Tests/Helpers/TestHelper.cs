using System.IO;
using NUnit.Framework;
using SeqFlatFileImport.Core;

namespace SeqFlatFileImport.Tests.Helpers
{
    public static class TestHelper
    {
        public static string GetFilePath(string inputFileName)
        {
            return Path.Combine(
                Path.GetDirectoryName(typeof(TestHelper).Assembly.CodeBase.Substring(8)),
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