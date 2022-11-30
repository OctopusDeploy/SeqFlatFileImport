using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lib.FileFormats
{
    public class OctopusServer : IFileFormat
    {
        /// <summary>
        /// Octopus 3.x -> 3.15.x
        /// </summary>
        private static readonly Regex LineRegex_3_0 =
            new(
                @"^(?<Timestamp>[0-9\-]{10} [0-9\:\.]{13}) +(?<Thread>[0-9]+) +(?<Level>[A-Z]+) +(?<Message>.+)$");

        /// <summary>
        /// We added PID in 3.15.x
        /// </summary>
        private static readonly Regex LineRegex_3_15 =
            new(
                @"^(?<Timestamp>[0-9\-]{10} [0-9\:\.]{13}) +(?<PID>[0-9]+) +(?<Thread>[0-9]+) +(?<Level>[A-Z]+) +(?<Message>.+)$");

        private static readonly Regex ExceptionRegex = new(@"Exception \(0x[0-9]+\):");
        private const RegexOptions DefaultOptions = RegexOptions.Singleline;

        private static readonly TemplateRegex[] TemplateRegexes =
        {
            new(
                new Regex(
                    @"^Reader took (?<Elapsed>[0-9]+)ms \((?<FirstRecord>[0-9]+)ms until the first record\) in transaction '(?<Transaction>.*)': (?<Query>.*)",
                    DefaultOptions),
                "Reader took {Elapsed}ms ({FirstRecord}ms until the first record) in transaction '{Transaction}': {Query}"
            ),
            new(
                new Regex(@"^(?<Method>[A-Z]+)\s+(?<Url>http.*) (?<CorrelationId>.*)", DefaultOptions),
                "{Method} {Url} {CorrelationId}"
            ),
            new(
                new Regex(@"^Request took (?<Elapsed>[0-9]+)ms: (?<Method>[A-Z]+)\s+(?<Url>http.*) (?<CorrelationId>.*)",
                    DefaultOptions),
                "Request took {Elapsed}ms: {Method} {Url} {CorrelationId}"
            ),
            new(
                new Regex(@"^(?<Operation>[A-Za-z]+) took (?<Elapsed>[0-9]+)ms: (?<Query>.*)", DefaultOptions),
                "{Operation} took {Elapsed}ms: {Query}"
            ),
            new(
                new Regex(@"^Unhandled exception from web server: (?<Message>.*)", DefaultOptions),
                "Unhandled exception from web server:  {Message}"
            ),
            new(
                new Regex(@"^listen://(?<IP>.+):(?<Port>[0-9]+)/ +[0-9]+ +(?<Message>.*)", DefaultOptions),
                "listen://{IP}:{Port}/ {Message}"
            ),
            new(
                new Regex(@"^poll://(?<Id>[a-z0-9]+)/ +[0-9]+ +(?<Message>.*)", DefaultOptions),
                "poll://{Id}/ {Message}"
            ),
            new(
                new Regex(@"^https://(?<Host>[^:]+):(?<Port>[0-9]+)/ +[0-9]+ +Retry attempt (?<n>[0-9]+)",
                    DefaultOptions),
                "https://{Host}:{Port}/ Retry attempt {n}"
            ),
            new(
                new Regex(@"^https://(?<Host>[^:]+):(?<Port>[0-9]+)/ +[0-9]+ +Opening a new connection$",
                    DefaultOptions),
                "https://{Host}:{Port}/ Opening a new connection"
            ),
            new(
                new Regex(@"^https://(?<Host>[^:]+):(?<Port>[0-9]+)/ +[0-9]+ + Performing TLS handshake$",
                    DefaultOptions),
                "https://{Host}:{Port}/ Performing TLS handshake"
            ),
            new(
                new Regex(
                    @"^https://(?<Host>[^:]+):(?<Port>[0-9]+)/ +[0-9]+ +Secure connection established. Server at (?<Endpoint>[^ ]+) identified by thumbprint: (?<Thumbprint>[A-Z0-9]+), using protocol (?<Protocol>[A-Za-z0-9]+)",
                    DefaultOptions),
                "https://{Host}:{Port}/ Secure connection established. Server at {Endpoint} identified by thumbprint: {Thumbprint}, using protocol {Protocol}"
            ),
            new(
                new Regex(
                    @"^https://(?<Host>[^:]+):(?<Port>[0-9]+)/ +[0-9]+ +No connection could be made because the target machine actively refused it (?<Endpoint>.+)$",
                    DefaultOptions),
                "https://{Host}:{Port}/ No connection could be made because the target machine actively refused it {Endpoint}"
            ),
            new(
                new Regex(@"^https://(?<Host>[^:]+):(?<Port>[0-9]+)/ +[0-9]+ +(?<Message>.*)", DefaultOptions),
                "https://{Host}:{Port}/ {Message}"
            ),
            new(
                new Regex(
                    @"""(?<Protocol>[A-Z]+)"" ""(?<Method>[A-Z]+)"" to ""(?<Url>.*)"" completed with (?<Code>[0-9]+) in .* \((?<Elapsed>[0-9]+.[0-9]+)ms\)",
                    DefaultOptions),
                "{Protocol} {Method} to {Url} completed with {Code} in {Elapsed}"
            ),
            new(
                new Regex(
                    @"^Execute reader took (?<Elapsed>[0-9]+)ms in transaction '(?<Transaction>.*)': (?<Query>.*)",
                    DefaultOptions),
                "Execute reader took {Elapsed} in transaction {Transaction}: {Query}"
            )
        };

        public string Name => "OctopusServer";
        public IReadOnlyList<string> AutodetectFileNameRegexes { get; } = new[] {@"OctopusServer.*\.txt"};
        public int Ordinal => 0;

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
                        var strVal = match.Groups[name].Value;
                        if (int.TryParse(strVal, out var intVal))
                            properties[name] = intVal;
                        else if (float.TryParse(strVal, out var floatVal))
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
            return str switch
            {
                "FATAL" => "Fatal",
                "ERROR" => "Error",
                "WARN" => "Warning",
                "DEBUG" => "Debug",
                "TRACE" => "Verbose",
                _ => "Information"
            };
        }

        private class TemplateRegex
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