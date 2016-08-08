using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SeqFlatFileImport.FileFormats
{
    public class OctopusTask : IFileFormat
    {
        public string Name { get; } = "OctopusTask";
        public IReadOnlyList<string> AutodetectFileNameRegexes { get; } = new[] { @"servertasks-\d+.*txt" };
        public int Ordinal { get; } = 0;
        public bool AutodetectFromContents(string[] firstFewLines)
        {
            if (firstFewLines.Length == 0)
                return false;

            return firstFewLines[0].StartsWith("[\"ServerTask");
        }

        public IEnumerable<RawEvent> Read(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                
                var json = JsonConvert.DeserializeObject(line) as ICollection<JToken>;

                if (json == null)
                    throw new Exception("Unexpected line: " + line);

                yield return new RawEvent
                {
                    Level = ParseLevel(json.ElementAt(1).ToString()),
                    MessageTemplate = json.ElementAt(3).ToString(),
                    Timestamp = ParseTime(json.ElementAt(2).ToString()),
                    Properties = ParseProperties(json.ElementAt(0).ToString())
                };
            }
        }

        private static DateTimeOffset ParseTime(string value)
        {
            return DateTimeOffset.Parse(value);
        }

        private static string ParseLevel(string value)
        {
            switch (value)
            {
                case "INF":
                    return "Information";
                case "VBS":
                    return "Verbose";
                case "WRN":
                    return "Warning";
                case "ERR":
                    return "Error";
                case "FAT":
                    return "Fatal";
                default:
                    return value;
            }
        }

        private static readonly Regex TaskId = new Regex(@"^(?<TaskId>\w+-\d+)");

        private static Dictionary<string, object> ParseProperties(string correlationId)
        {
            var match = TaskId.Match(correlationId);

            return new Dictionary<string, object> {
                { "Task ID", match.Groups["TaskId"].Value }
            };
        }
    }
}