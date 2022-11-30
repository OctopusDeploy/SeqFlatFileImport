using System.Collections.Generic;
using NDesk.Options;

namespace SeqFlatFileImport
{
    internal class Options
    {
        private Options()
        {
        }

        public IReadOnlyList<string> InputPaths { get; private set; }
        public string SeqServer { get; private set; }
        public string SeqApiKey { get; private set; }
        public string Format { get; private set; }
        public string BatchId { get; private set; }

        public static Options Parse(string[] args)
        {
            var options = new Options();
            var help = false;
            var optionSet = new OptionSet
            {
                {"h|?|help", v => help = v != null},
                {"server=", v => options.SeqServer = v},
                {"apikey=", v => options.SeqApiKey = v},
                {"format=", v => options.Format = v},
                {"batch=", v => options.BatchId = v}
            };

            options.InputPaths = optionSet.Parse(args);
            if (help)
            {
                optionSet.WriteOptionDescriptions(System.Console.Out);
                return null;
            }

            if (options.InputPaths.Count == 0)
            {
                System.Console.Error.WriteLine("No input files or directories specified");
                return null;
            }

            return options;
        }

    }
}