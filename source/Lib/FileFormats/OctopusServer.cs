using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SeqFlatFileImport.FileFormats
{
    public class OctopusServer : IFileFormat
    {
        /// <summary>
        /// Octopus 3.x -> 3.15.x
        /// </summary>
        private static readonly Regex LineRegex_3_0 =
            new Regex(
                @"^(?<Timestamp>[0-9\-]{10} [0-9\:\.]{13}) +(?<Thread>[0-9]+) +(?<Level>[A-Z]+) +(?<Message>.+)$");

        /// <summary>
        /// We added PID in 3.15.x
        /// </summary>
        private static readonly Regex LineRegex_3_15 =
            new Regex(
                @"^(?<Timestamp>[0-9\-]{10} [0-9\:\.]{13}) +(?<PID>[0-9]+) +(?<Thread>[0-9]+) +(?<Level>[A-Z]+) +(?<Message>.+)$");

        private static readonly Regex ExceptionRegex = new Regex(@"Exception \(0x[0-9]+\):");
        private static readonly RegexOptions DefaultOptions = RegexOptions.Singleline;

        private static readonly TemplateRegex[] TemplateRegexes =
        {
            new TemplateRegex(
                new Regex(
                    @"^Reader took (?<Time>[0-9]+)ms \((?<FirstRecord>[0-9]+)ms until the first record\) in transaction '(?<Transaction>.*)': (?<Query>.*)",
                    DefaultOptions),
                "Reader took {Time}ms ({FirstRecord}ms until the first record) in transaction '{Transaction}': {Query}"
            ),
            new TemplateRegex(
                new Regex(@"^(?<Method>[A-Z]+)\s+(?<Url>http.*) (?<CorrelationId>.*)", DefaultOptions),
                "{Method} {Url} {CorrelationId}"
            ),
            new TemplateRegex(
                new Regex(@"^Request took (?<Time>[0-9]+)ms: (?<Method>[A-Z]+)\s+(?<Url>http.*) (?<CorrelationId>.*)",
                    DefaultOptions),
                "Request took {Time}ms: {Method} {Url} {CorrelationId}"
            ),
            new TemplateRegex(
                new Regex(@"^(?<Operation>[A-Za-z]+) took (?<Time>[0-9]+)ms: (?<Query>.*)", DefaultOptions),
                "{Operation} took {Time}ms: {Query}"
            ),
            new TemplateRegex(
                new Regex(@"^Unhandled exception from web server: (?<Message>.*)", DefaultOptions),
                "Unhandled exception from web server:  {Message}"
            ),
            new TemplateRegex(
                new Regex(@"^listen://(?<IP>.+):(?<Port>[0-9]+)/ +[0-9]+ +(?<Message>.*)", DefaultOptions),
                "listen://{IP}:{Port}/ {Message}"
            ),
            new TemplateRegex(
                new Regex(@"^poll://(?<Id>[a-z0-9]+)/ +[0-9]+ +(?<Message>.*)", DefaultOptions),
                "poll://{Id}/ {Message}"
            ),
            new TemplateRegex(
                new Regex(@"^https://(?<Host>[^:]+):(?<Port>[0-9]+)/ +[0-9]+ +Retry attempt (?<n>[0-9]+)",
                    DefaultOptions),
                "https://{Host}:{Port}/ Retry attempt {n}"
            ),
            new TemplateRegex(
                new Regex(@"^https://(?<Host>[^:]+):(?<Port>[0-9]+)/ +[0-9]+ +Opening a new connection$",
                    DefaultOptions),
                "https://{Host}:{Port}/ Opening a new connection"
            ),
            new TemplateRegex(
                new Regex(@"^https://(?<Host>[^:]+):(?<Port>[0-9]+)/ +[0-9]+ + Performing TLS handshake$",
                    DefaultOptions),
                "https://{Host}:{Port}/ Performing TLS handshake"
            ),
            new TemplateRegex(
                new Regex(
                    @"^https://(?<Host>[^:]+):(?<Port>[0-9]+)/ +[0-9]+ +Secure connection established. Server at (?<Endpoint>[^ ]+) identified by thumbprint: (?<Thumbprint>[A-Z0-9]+), using protocol (?<Protocol>[A-Za-z0-9]+)",
                    DefaultOptions),
                "https://{Host}:{Port}/ Secure connection established. Server at {Endpoint} identified by thumbprint: {Thumbprint}, using protocol {Protocol}"
            ),
            new TemplateRegex(
                new Regex(
                    @"^https://(?<Host>[^:]+):(?<Port>[0-9]+)/ +[0-9]+ +No connection could be made because the target machine actively refused it (?<Endpoint>.+)$",
                    DefaultOptions),
                "https://{Host}:{Port}/ No connection could be made because the target machine actively refused it {Endpoint}"
            ),
            new TemplateRegex(
                new Regex(@"^https://(?<Host>[^:]+):(?<Port>[0-9]+)/ +[0-9]+ +(?<Message>.*)", DefaultOptions),
                "https://{Host}:{Port}/ {Message}"
            ),
            new TemplateRegex(
                new Regex(
                    @"""(?<Protocol>[A-Z]+)"" ""(?<Method>[A-Z]+)"" to ""(?<Url>.*)"" completed with (?<Code>[0-9]+) in .* \((?<Elapsed>[0-9]+.[0-9]+)ms\)",
                    DefaultOptions),
                "{Protocol} {Method} to {Url} completed with {Code} in {Elapsed}"
            )
        };

        public string Name { get; } = "OctopusServer";
        public IReadOnlyList<string> AutodetectFileNameRegexes { get; } = new[] {@"OctopusServer.*\.txt"};
        public int Ordinal { get; } = 0;

        public bool AutodetectFromContents(string[] firstFewLines)
        {
            return false;
        }

        public IEnumerable<RawEvent> Read(IEnumerable<string> lines)
        {
            var lineNumber = 0;
            Match currentLogEntryMatch = null;
            var currentLogEntryStartingLineNumber = lineNumber;
            var buffer = new List<string>();

            var lineRegex = new[] {LineRegex_3_0, LineRegex_3_15};

            foreach (var line in lines)
            {
                lineNumber++;
                var newLogEntryMatch =
                    lineRegex.Select(regex => regex.Match(line)).FirstOrDefault(match => match.Success);
                if (newLogEntryMatch?.Success == true)
                {
                    // Flush the existing entry (maybe we don't have one yet?)
                    if (currentLogEntryMatch != null)
                    {
                        yield return ProcessLogMessage(currentLogEntryStartingLineNumber, currentLogEntryMatch, buffer);
                        buffer.Clear();
                    }

                    // Start accumulating the new log entry
                    currentLogEntryMatch = newLogEntryMatch;
                    currentLogEntryStartingLineNumber = lineNumber;
                }
                else
                {
                    buffer.Add(line);
                }
            }

            // Flush the final entry
            if (currentLogEntryMatch != null)
            {
                yield return ProcessLogMessage(currentLogEntryStartingLineNumber, currentLogEntryMatch, buffer);
                buffer.Clear();
            }
        }

        private static RawEvent ProcessLogMessage(int lineNumber, Match currentLogEntryMatch,
            IReadOnlyCollection<string> lines)
        {
            var properties = new Dictionary<string, object>
            {
                {"LineNumber", lineNumber},
                {"PID", currentLogEntryMatch.Groups["PID"].Value},
                {"Thread", currentLogEntryMatch.Groups["Thread"].Value}
            };

            var messageLines = new[] {currentLogEntryMatch.Groups["Message"].Value}
                .Concat(lines.TakeWhile(line => !ExceptionRegex.Match(line).Success)).ToArray();
            var message = string.Join(Environment.NewLine, messageLines);
            var messageTemplate = MagicUpTheMessageTemplate(message, properties);

            var rawEvent = new RawEvent
            {
                Timestamp = DateTimeOffset.Parse(currentLogEntryMatch.Groups["Timestamp"].Value),
                Level = ConvertLevel(currentLogEntryMatch.Groups["Level"].Value),
                Properties = properties,
                MessageTemplate = messageTemplate
            };

            var exceptionLines = lines.Except(messageLines).ToArray();
            if (exceptionLines.Any())
            {
                rawEvent.Exception = string.Join(Environment.NewLine, exceptionLines);
            }

            return rawEvent;
        }

        private static string MagicUpTheMessageTemplate(string message, Dictionary<string, object> properties)
        {
            foreach (var tr in TemplateRegexes)
            {
                var match = tr.Regex.Match(message);
                if (match.Success)
                {
                    foreach (var name in tr.Regex.GetGroupNames().Where(n => n != "0"))
                    {
                        string strVal = match.Groups[name].Value;
                        int intVal;
                        if (int.TryParse(strVal, out intVal))
                            properties[name] = intVal;
                        if (float.TryParse(strVal, out var floatVal))
                            properties[name] = floatVal;
                        else
                            properties[name] = strVal;
                    }


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
                case "TRACE":
                    return "Verbose";
                default:
                    return "Information";
            }
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