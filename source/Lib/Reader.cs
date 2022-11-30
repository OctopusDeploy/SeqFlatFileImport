using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Lib
{
    internal class Reader : IDisposable
    {
        private readonly StreamReader _sr;

        internal Reader(Stream stream)
        {
            stream.Seek(0, SeekOrigin.Begin);
            _sr = new StreamReader(stream, Encoding.UTF8, true, 1024, true);
        }

        public IEnumerable<string> Enumerable()
        {
            while (!_sr.EndOfStream)
                yield return _sr.ReadLine();
        }

        public void Dispose()
        {
            _sr.Dispose();
        }

    }
}