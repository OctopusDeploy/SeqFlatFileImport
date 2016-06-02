using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SeqFlatFileImport.Tests.Helpers
{
    public class StubSeqEndpoint : ISeqEndpoint
    {
        private readonly List<RawEvent> _logs = new List<RawEvent>();
        public string LogsAsJson => JsonConvert.SerializeObject(_logs);
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