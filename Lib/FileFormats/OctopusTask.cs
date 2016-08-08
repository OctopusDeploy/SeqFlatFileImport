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
                    Properties = ParseProperties(json.ElementAt(0).ToString(), json.ElementAt(3).ToString())
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
        private readonly IDictionary<string, string> correlationProperties = new Dictionary<string, string>();

        private Dictionary<string, object> ParseProperties(string correlationId, string message)
        {
            var properties = new Dictionary<string, object>();

            var tokens = correlationId.Split('/');

            var match = TaskId.Match(tokens[0]);
            properties.Add("Task ID", match.Groups["TaskId"].Value);

            for (var i = 1; i < tokens.Length; i++)
            {
                var token = tokens[i];
                string value;
                if (correlationProperties.TryGetValue(token, out value))
                {
                    properties[$"Property {i}"] = value;
                }
                else
                {
                    properties[$"Property {i}"] = message;
                    correlationProperties[token] = message;
                }
            }

            return properties;
        }
    }
}