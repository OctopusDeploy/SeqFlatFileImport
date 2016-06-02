using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SeqFlatFileImport.FileFormats
{
    public class Iis : IFileFormat
    {
        public virtual string Name { get; } = "IIS";
        public virtual IReadOnlyList<string> AutodetectFileNameRegexes { get; } = new[]
        {
            @"u_ex[0-9]{6}\.log"
        };

        public int Ordinal { get; } = 0;
        public bool AutodetectFromContents(string[] firstFewLines)
        {
            if (firstFewLines.Length == 0)
                return false;
            return firstFewLines[0].StartsWith("#Software: Microsoft Internet Information Services");
        }

        public IEnumerable<RawEvent> Read(IEnumerable<string> lines)
        {
            var fieldNames = new string[0];
            var template = "";
            
            foreach (var line in lines.Where(l => l.Length > 0))
            {
                if (line[0] == '#')
                {
                    if (line.StartsWith("#Fields"))
                    {
                        fieldNames = line.Split(' ').Skip(1).ToArray();
                        template = GetTemplate(fieldNames);
                    }
                }
                else
                {
                    var fields = line.Split(' ');
                    var properties = new Dictionary<string, object>();
                    for (var x = 0; x < fields.Length; x++)
                        properties[fieldNames.Length > x ? fieldNames[x] : x.ToString()] = fields[x];


                    yield return new RawEvent()
                    {
                        Timestamp = GetTimestamp(properties),
                        Level = "Information",
                        MessageTemplate = template,
                        Properties = properties
                    };
                }
            }
        }

        private string GetTemplate(string[] fieldNames)
        {
            var sb = new StringBuilder();
            if (fieldNames.Contains("cs-method"))
                sb.Append("{cs-method}");
            if (fieldNames.Contains("time-taken"))
                sb.Append(" {time-taken}");
            if (fieldNames.Contains("sc-status"))
            {
                sb.Append("{sc-status}");
                if (fieldNames.Contains("sc-substatus"))
                    sb.Append(".{sc-substatus}");
            }
            if (fieldNames.Contains("cs-uri-stem"))
                sb.Append(" {cs-uri-stem} ");
            if (fieldNames.Contains("cs-uri-query"))
                sb.Append(" {cs-uri-query} ");
            return sb.ToString().Trim();
        }

        private DateTimeOffset GetTimestamp(Dictionary<string, object> properties)
        {
            if (!properties.ContainsKey("date"))
                return DateTimeOffset.Now;

            var datetime = properties.ContainsKey("time") ? DateTime.Parse(properties["date"] + " " + properties["time"]) : DateTime.Parse(""+properties["date"]);
            return new DateTimeOffset(datetime, TimeSpan.Zero);
        }
    }
}