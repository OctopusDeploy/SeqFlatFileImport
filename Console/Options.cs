using System;
using System.Collections.Generic;
using System.Linq;
using NDesk.Options;

namespace SeqFlatFileImport
{
    internal class Options
    {
        private Options()
        {
        }

        public IReadOnlyList<string> InputFiles { get; private set; }
        public string SeqServer { get; private set; }
        public string SeqApiKey { get; private set; }
        public string Format { get; private set; }
        public string BatchId { get; set; }

        public static Options Parse(string[] args)
        {
            var options = new Options();
            var help = false;
            var optionSet = new OptionSet()
            {
                {"h|?|help", v => help = v != null},
                {"server=", v => options.SeqServer = v},
            //    {"apikey=", v => options.SeqApiKey = v},
                {"format=", v => options.Format = v},
                {"batch=", v => options.BatchId = v}
            };

            options.InputFiles = optionSet.Parse(args);
            if (help)
            {
                optionSet.WriteOptionDescriptions(Console.Out);
                return null;
            }

            if (options.InputFiles.Count == 0)
            {
                Console.Error.WriteLine("No input files specified");
                return null;
            }

            return options;
        }

    }
}