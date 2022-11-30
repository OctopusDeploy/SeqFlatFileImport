using System.Collections.Generic;
using Lib;
using Newtonsoft.Json;

namespace Tests.Helpers
{
    public class StubSeqEndpoint : ISeqEndpoint
    {
        private readonly List<RawEvent> _logs = new List<RawEvent>();
        public string LogsAsJson => JsonConvert.SerializeObject(_logs, Formatting.Indented);
        public void Write(RawEvent entry)
        {
            _logs.Add(entry);
        }

        public IResult Flush()
        {
            return Result.Success();
        }
    }
}