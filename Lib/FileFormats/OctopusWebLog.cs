using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SeqFlatFileImport.FileFormats
{
    public class OctopusWebLog : IFileFormat
    {
        public string Name { get; } = "OctopusWebLog";

        public IReadOnlyList<string> AutodetectFileNameRegexes { get; } = new[]
        {
            @"Web-[0-9]{4}-[0-9]{2}-[0-9]{2}\.log"
        };

        public int Ordinal { get; } = 0;
        public bool AutodetectFromContents(string[] firstFewLines)
        {
            if (firstFewLines.Length == 0)
                return false;
            return firstFewLines[0].StartsWith("#Software: Octopus Deploy");
        }

        public IEnumerable<RawEvent> Read(IEnumerable<string> lines)
        {
            var fieldNames = new string[0];

            foreach (var line in lines.Where(l => l.Length > 0))
            {
                if (line[0] == '#')
                {
                    if (line.StartsWith("#Fields"))
                    {
                        fieldNames = line.Split(' ').Skip(1).ToArray();
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
                        MessageTemplate = "{cs-method} {time-taken} {sc-status} {cs-uri-stem} {cs-username} {c-ip}",
                        Properties = properties
                    };
                }
            }
        }


        private DateTimeOffset GetTimestamp(Dictionary<string, object> properties)
        {
            return new DateTimeOffset(DateTime.Parse(properties["date"] + " " + properties["time"]), TimeSpan.Zero);
        }
    }
}