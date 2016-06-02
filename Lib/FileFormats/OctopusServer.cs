using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Schema;

namespace SeqFlatFileImport.FileFormats
{
    public class OctopusServer : IFileFormat
    {
        private static readonly Regex LineRegex = new Regex(@"^([0-9\-]{10} [0-9\:\.]{13}) +([0-9]+) +([A-Z]+) +(.+)$");
        private static readonly Regex ExceptionRegex = new Regex(@"Exception \(0x[0-9]+\):");

        private static readonly TemplateRegex[] TemplateRegexes = new[]
        {
            new TemplateRegex(
                new Regex(
                    @"^Reader took (?<Time>[0-9]+)ms \((?<FirstRecord>[0-9]+)ms until the first record\): (?<Query>.*)$"),
                "Reader took {Time}ms ({FirstRecord}ms until the first record): {Query}"
                ),
            new TemplateRegex(
                new Regex(@"^Request took (?<Time>[0-9]+)ms: (?<Query>.*)$"),
                "Request took {Time}ms: {Query}"
                ),
            new TemplateRegex(
                new Regex(@"^(?<Operation>[A-Za-z]+) took (?<Time>[0-9]+)ms: (?<Query>.*)$"),
                "{Operation} took {Time}ms: {Query}"
                ),
            new TemplateRegex(
                new Regex(@"^Unhandled exception from web server: (?<Message>.*)$"),
                "Unhandled exception from web server:  {Message}"
                ),
            new TemplateRegex(
                new Regex(@"^listen://(?<IP>.+):(?<Port>[0-9]+)/ +[0-9]+ +(?<Message>.*)$"),
                "listen://{IP}:{Port}/ {Message}"
                ),
            new TemplateRegex(
                new Regex(@"^poll://(?<Id>[a-z0-9]+)/ +[0-9]+ +(?<Message>.*)$"),
                "poll://{Id}/ {Message}"
                ),
            new TemplateRegex(
                new Regex(@"^https://(?<Host>[^:]+):(?<Port>[0-9]+)/ +[0-9]+ +Retry attempt (?<n>[0-9]+)$"),
                "https://{Host}:{Port}/ Retry attempt {n}"
                ),
            new TemplateRegex(
                new Regex(@"^https://(?<Host>[^:]+):(?<Port>[0-9]+)/ +[0-9]+ +Opening a new connection$"),
                "https://{Host}:{Port}/ Opening a new connection"
                ),
              new TemplateRegex(
                new Regex(@"^https://(?<Host>[^:]+):(?<Port>[0-9]+)/ +[0-9]+ + Performing TLS handshake$"),
                "https://{Host}:{Port}/ Performing TLS handshake"
                ),
             new TemplateRegex(
                new Regex(@"^https://(?<Host>[^:]+):(?<Port>[0-9]+)/ +[0-9]+ +Secure connection established. Server at (?<Endpoint>[^ ]+) identified by thumbprint: (?<Thumbprint>[A-Z0-9]+), using protocol (?<Protocol>[A-Za-z0-9]+)"),
                "https://{Host}:{Port}/ Secure connection established. Server at {Endpoint} identified by thumbprint: {Thumbprint}, using protocol {Protocol}"
                ),
            new TemplateRegex(
                new Regex(@"^https://(?<Host>[^:]+):(?<Port>[0-9]+)/ +[0-9]+ +No connection could be made because the target machine actively refused it (?<Endpoint>.+)$"),
                "https://{Host}:{Port}/ No connection could be made because the target machine actively refused it {Endpoint}"
                ),
            new TemplateRegex(
                new Regex(@"^https://(?<Host>[^:]+):(?<Port>[0-9]+)/ +[0-9]+ +(?<Message>.*)$"),
                "https://{Host}:{Port}/ {Message}"
                )
        };

        public string Name { get; } = "OctopusServer";
        public IReadOnlyList<string> AutodetectFileNameRegexes { get; } = new[] { @"OctopusServer.*\.txt" };
        public int Ordinal { get; } = 0;

        public bool AutodetectFromContents(string[] firstFewLines)
        {
            return false;
        }

        public IEnumerable<RawEvent> Read(IEnumerable<string> lines)
        {
            RawEvent currentEvent = null;
            var inException = false;
            var lineNumber = 0;

            foreach (var line in lines)
            {
                lineNumber++;
                var match = LineRegex.Match(line);
                if (match.Success)
                {
                    inException = false;
                    if (currentEvent != null)
                        yield return currentEvent;

                    currentEvent = CreateEventFromMatch(lineNumber, match);
                }
                else if (currentEvent != null)
                {
                    if (inException || ExceptionRegex.IsMatch(line))
                    {
                        currentEvent.Exception += line;
                        inException = true;
                    }
                    else
                    {
                        currentEvent.MessageTemplate += "\r\n" + line;
                    }
                }
                else
                {
                    yield return UnexpectedLine(lineNumber, line);
                }
            }
            if (currentEvent != null)
                yield return currentEvent;
        }

        private static RawEvent CreateEventFromMatch(int lineNumber, Match match)
        {
            var properties = new Dictionary<string, object>()
            {
                {"LineNumber", lineNumber },
                {"Thread", match.Groups[2].Value}
            };

            var messageTemplate = MagicUpTheMessageTemplate(match.Groups[4].Value, properties);

            return new RawEvent
            {
                Timestamp = DateTimeOffset.Parse(match.Groups[1].Value),
                Level = ConvertLevel(match.Groups[3].Value),
                Properties = properties,
                MessageTemplate = messageTemplate
            };
        }

        private static string MagicUpTheMessageTemplate(string message, Dictionary<string, object> properties)
        {
            foreach (var tr in TemplateRegexes)
            {
                var match = tr.Regex.Match(message);
                if (match.Success)
                {
                    foreach (var name in tr.Regex.GetGroupNames().Where(n => n != "0"))
                        properties[name] = match.Groups[name].Value;

                    return tr.Template;
                }
            }
            return message;
        }

        private static string ConvertLevel(string str)
        {
            switch (str)
            {
                case "FATAL":
                    return "Fatal";
                case "ERROR":
                    return "Error";
                case "WARN":
                    return "Warning";
                case "DEBUG":
                    return "Debug";
                default:
                    return "Information";
            }
        }

        private static RawEvent UnexpectedLine(int lineNumber, string line)
        {
            return new RawEvent()
            {
                Timestamp = DateTimeOffset.Now,
                Level = "Warning",
                MessageTemplate = "Unexpected line while parsing log {LineNumber}: {Line}",
                Properties = new Dictionary<string, object>()
                {
                    {"LineNumber", lineNumber},
                    {"Line", line}
                }
            };
        }


        class TemplateRegex
        {
            public Regex Regex { get; }
            public string Template { get; }

            public TemplateRegex(Regex regex, string template)
            {
                Regex = regex;
                Template = template;
            }
        }
    }
}