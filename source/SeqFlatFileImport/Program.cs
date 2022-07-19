using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SeqFlatFileImport.Core;

namespace SeqFlatFileImport
{
    class Program
    {
        static int Main(string[] args)
        {
            var options = Options.Parse(args);
            if (options == null)
                return 1;

            if (!CheckFilesOrDirectoriesExist(options.InputPaths))
                return 2;

            var inputFiles = GetFiles(options.InputPaths);
            var importer = new Importer(seqServer: options.SeqServer, seqApiKey: options.SeqApiKey, progressCallback: Console.WriteLine, batchId: options.BatchId);
            foreach (var file in inputFiles)
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


        private static bool CheckFilesOrDirectoriesExist(IReadOnlyList<string> inputFiles)
        {
            var notExist = inputFiles.Where(f => !File.Exists(f) && !Directory.Exists(f)).ToArray();
            if (!notExist.Any())
                return true;

            WriteError("The following paths could not be found:");
            WriteError(notExist);
            return false;
        }

        private static IReadOnlyList<string> GetFiles(IReadOnlyList<string> inputPaths)
        {
            var files = inputPaths.Where(File.Exists).ToArray();
            var filesInDirectory = inputPaths.Except(files)
                .Where(Directory.Exists)
                .SelectMany(Directory.EnumerateFiles);

            return files.Concat(filesInDirectory).ToList();
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
