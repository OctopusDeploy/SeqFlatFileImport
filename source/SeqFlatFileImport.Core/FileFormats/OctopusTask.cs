using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SeqFlatFileImport.Core.FileFormats
{
    public class OctopusTask : IFileFormat
    {
        private const string HeaderStart = "                    |";
        private static readonly Regex messageRegex = new Regex(@"^(?<Time>[0-9\:]{8})? +(?<Level>[A-Za-z]+) +\| +(?<Message>.+)$");
        public string Name { get; } = "OctopusTask";
        public IReadOnlyList<string> AutodetectFileNameRegexes { get; } = new[] { @"ServerTasks-.*" };
        public int Ordinal { get; } = 0;
        public bool AutodetectFromContents(string[] firstFewLines)
        {
            if (firstFewLines.Length == 0)
                return false;
            return firstFewLines[0].StartsWith("Task ID:");
        }





        public IEnumerable<RawEvent> Read(IEnumerable<string> lines)
        {
            var header = GetHeader(lines);
            if (header.Queued.HasValue)
                yield return new RawEvent()
                {
                    Level = "Information",
                    MessageTemplate = "Task {TaskID} queued",
                    Timestamp = new DateTimeOffset(header.Queued.Value),
                    Properties = header.Properties
                };
            yield return new RawEvent()
            {
                Level = "Information",
                MessageTemplate = "Task {TaskID} started",
                Timestamp = new DateTimeOffset(header.Started),
                Properties = header.Properties
            };

            var properties = new Dictionary<string, object>()
            {
                {"Task ID", header.Id}
            };

            foreach (var line in lines)
            {
                if (line.StartsWith(HeaderStart))
                {
                    var section = line.Substring(HeaderStart.Length).Trim();
                    foreach (var parser in SectionParsers)
                        if (parser(section, properties))
                            break;
                }
                else
                {
                    var match = messageRegex.Match(line);
                    if (!match.Success)
                        throw new Exception("Unexpected line: " + line);

                    yield return new RawEvent()
                    {
                        Level = ParseLevel(match.Groups["Level"].Value),
                        Timestamp = ParseTime(match.Groups["Time"].Value, header),
                        MessageTemplate = match.Groups["Message"].Value,
                        Properties = new Dictionary<string, object>(properties)
                    };
                }
            }

        }

        private readonly Func<string, Dictionary<string, object>, bool>[] SectionParsers = new Func<string, Dictionary<string, object>, bool>[]
        {
            OverallSection,
            StepSection,
            AquirePackagesSection,
            UploadPackagesSection,
            MachineSection
        };

        private static bool OverallSection(string section, Dictionary<string, object> properties)
        {
            var regex = new Regex("^== (?<Status>[A-Za-z]+): (?<Project>.*) release (?<Release>[^ ]+) to (?<Environment>.*) ==$");
            var match = regex.Match(section);
            if (match.Success)
                CopyMatchGroupsToProperties(regex, match, properties);
            return match.Success;
        }

        private static bool StepSection(string section, Dictionary<string, object> properties)
        {
            var regex = new Regex("^== (?<Status>[A-Za-z]+): Step (?<Step>[0-9]+): (?<StepName>.*) ==$");
            var match = regex.Match(section);
            if (match.Success)
            {
                CopyMatchGroupsToProperties(regex, match, properties);
                properties.Remove("Machine");
                properties.Remove("PackageName");
                properties.Remove("PackageVersion");
            }
            return match.Success;
        }
        private static bool AquirePackagesSection(string section, Dictionary<string, object> properties)
        {
            var regex = new Regex("^== (?<Status>[A-Za-z]+): Acquire packages ==$");
            var match = regex.Match(section);
            if (match.Success)
            {
                CopyMatchGroupsToProperties(regex, match, properties);
                properties.Remove("StepName");
                properties.Remove("Step");
                properties.Remove("Machine");
            }
            return match.Success;
        }
        private static bool UploadPackagesSection(string section, Dictionary<string, object> properties)
        {
            var regex = new Regex("^(?<Status>[A-Za-z]+): Upload package (?<PackageName>[^ ]+) version (?<PackageVersion>[^ ]+)$");
            var match = regex.Match(section);
            if (match.Success)
                CopyMatchGroupsToProperties(regex, match, properties);
            return match.Success;
        }
        private static bool MachineSection(string section, Dictionary<string, object> properties)
        {
            var regex = new Regex("^(?<Status>[A-Za-z]+): (?<Machine>.*)$");
            var match = regex.Match(section);
            if (match.Success)
                CopyMatchGroupsToProperties(regex, match, properties);
            return match.Success;
        }

        private static void CopyMatchGroupsToProperties(Regex regex, Match match, Dictionary<string, object> properties)
        {
            foreach (var name in regex.GetGroupNames().Where(n => n != "0"))
                properties[name] = match.Groups[name].Value;
        }

        private string ParseLevel(string value)
        {
            switch (value)
            {
                case "Info":
                    return "Information";
                default:
                    return value;
            }
        }

        private static DateTimeOffset ParseTime(string value, Header header)
        {
            var time = TimeSpan.Parse(value);
            var timestamp = (header.Started.TimeOfDay > time ? header.Started.Date.AddDays(1) : header.Started.Date).Add(time);
            return new DateTimeOffset(timestamp);
        }

        private Header GetHeader(IEnumerable<string> lines)
        {
            var regex = new Regex("^([A-Za-z ]+): +(.+)$");
            var header = new Header();
            foreach (var line in lines)
            {
                if (line.Trim().Length == 0)
                    return header;

                var match = regex.Match(line);
                if (!match.Success)
                    throw new Exception("Unexpected line: " + line);

                var key = match.Groups[1].Value;
                var value = match.Groups[2].Value;
                header.Properties[key.Replace(" ", "")] = value;

                switch (key)
                {
                    case "Task queued":
                        header.Queued = DateTime.Parse(value);
                        break;
                    case "Task started":
                        header.Started = DateTime.Parse(value);
                        break;
                    case "Task ID":
                        header.Id = value.Substring("ServerTasks-".Length);
                        break;
                }

            }
            return header;
        }

        class Header
        {
            public DateTime? Queued { get; set; }
            public DateTime Started { get; set; }
            public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();
            public string Id { get; set; }
        }
    }
}