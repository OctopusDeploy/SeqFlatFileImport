using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lib.FileFormats
{
    public class OctopusRawTask : IFileFormat
    {
        public string Name => "OctopusRawTask";
        public IReadOnlyList<string> AutodetectFileNameRegexes { get; } = new[] { @"servertasks-\d+.*txt" };
        public int Ordinal => 0;

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
            return DateTimeOffset.Parse(value).UtcDateTime;
        }

        private static string ParseLevel(string value)
        {
            return value switch
            {
                "INF" => "Information",
                "VBS" => "Verbose",
                "WRN" => "Warning",
                "ERR" => "Error",
                "FAT" => "Fatal",
                _ => value
            };
        }

        private static readonly Regex TaskId = new(@"^(?<TaskId>\w+-\d+)");
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
                if (correlationProperties.TryGetValue(token, out var value))
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