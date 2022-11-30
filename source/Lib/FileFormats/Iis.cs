using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lib.FileFormats
{
    public class Iis : IFileFormat
    {
        public virtual string Name => "IIS";

        public virtual IReadOnlyList<string> AutodetectFileNameRegexes { get; } = new[]
        {
            @"u_ex[0-9]{6}\.log"
        };

        public int Ordinal => 0;

        public bool AutodetectFromContents(string[] firstFewLines)
        {
            if (firstFewLines.Length == 0)
                return false;
            return firstFewLines[0].StartsWith("#Software: Microsoft Internet Information Services");
        }

        public IEnumerable<RawEvent> Read(IEnumerable<string> lines)
        {
            var fieldNames = Array.Empty<string>();
            var template = "";
            
            foreach (var line in lines.Where(l => l.Length > 0))
            {
                if (line[0] == '#')
                {
                    if (line.StartsWith("#Fields"))
                    {
                        fieldNames = line.Split(' ').Skip(1).Select(s => s.Replace("-", "_")).ToArray();
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
            if (fieldNames.Contains("cs_method"))
                sb.Append("{cs_method}");
            if (fieldNames.Contains("time_taken"))
                sb.Append(" {time_taken}");
            if (fieldNames.Contains("sc_status"))
            {
                sb.Append("{sc_status}");
                if (fieldNames.Contains("sc_substatus"))
                    sb.Append(".{sc_substatus}");
            }
            if (fieldNames.Contains("cs_uri_stem"))
                sb.Append(" {cs_uri_stem} ");
            if (fieldNames.Contains("cs_uri_query"))
                sb.Append(" {cs_uri_query} ");
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