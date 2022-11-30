using System;
using System.Collections.Generic;
using System.Linq;

namespace SeqFlatFileImport.Core.FileFormats
{
    public class OctopusWebLog : IFileFormat
    {
        public string Name => "OctopusWebLog";

        public IReadOnlyList<string> AutodetectFileNameRegexes { get; } = new[]
        {
            @"Web-[0-9]{4}-[0-9]{2}-[0-9]{2}\.log"
        };

        public int Ordinal => 0;

        public bool AutodetectFromContents(string[] firstFewLines)
        {
            return firstFewLines.Length != 0 && firstFewLines[0].StartsWith("#Software: Octopus Deploy");
        }

        public IEnumerable<RawEvent> Read(IEnumerable<string> lines)
        {
            var fieldNames = Array.Empty<string>();

            foreach (var line in lines.Where(l => l.Length > 0))
            {
                if (line[0] == '#')
                {
                    if (line.StartsWith("#Fields"))
                    {
                        fieldNames = line.Split(' ').Skip(1).Select(s => s.Replace("-", "_")).ToArray();
                    }
                }
                else
                {
                    var fields = line.Split('\t');
                    var properties = new Dictionary<string, object>();
                    for (var x = 0; x < fields.Length; x++)
                        properties[fieldNames.Length > x ? fieldNames[x] : x.ToString()] = fields[x];


                    yield return new RawEvent()
                    {
                        Timestamp = GetTimestamp(properties),
                        Level = "Information",
                        MessageTemplate = "{cs_method} {time_taken} {sc_status} {cs_uri_stem} {cs_username} {c_ip}",
                        Properties = properties
                    };
                }
            }
        }


        private static DateTimeOffset GetTimestamp(Dictionary<string, object> properties)
        {
            return new DateTimeOffset(DateTime.Parse(properties["date"] + " " + properties["time"]), TimeSpan.Zero);
        }
    }
}