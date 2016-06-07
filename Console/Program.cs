using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SeqFlatFileImport
{
    class Program
    {
        static int Main(string[] args)
        {
            var options = Options.Parse(args);
            if (options == null)
                return 1;

            if (!CheckFilesExist(options.InputFiles))
                return 2;
            var importer = new Importer(seqServer: options.SeqServer, seqApiKey: options.SeqApiKey, progressCallback: Console.WriteLine, batchId: options.BatchId);
            foreach (var file in options.InputFiles)
            {
                WriteLine(ConsoleColor.White, $"Importing {file}... ");
                var result = importer.Import(file, options.Format);
                if (result.WasSuccessful)
                {
                    WriteLine(ConsoleColor.Green, "Success");
                }
                else
                {
                    WriteLine(ConsoleColor.Red, "Failed");
                    WriteError(result.Errors);
                    return 3;
                }
            }

            return 0;
        }


        private static bool CheckFilesExist(IReadOnlyList<string> inputFiles)
        {
            var notExist = inputFiles.Where(f => !File.Exists(f)).ToArray();
            if (!notExist.Any())
                return true;

            WriteError("The following files could not be found:");
            WriteError(notExist);
            return false;
        }

        private static void WriteLine(ConsoleColor colour, string str)
        {
            Console.ForegroundColor = colour;
            Console.WriteLine(str);
            Console.ResetColor();
        }

        private static void WriteError(params string[] errors)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            foreach (var error in errors)
                Console.Error.WriteLine(error);
            Console.ResetColor();
        }
    }
}
