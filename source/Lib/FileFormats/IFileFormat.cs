using System.Collections.Generic;

namespace Lib.FileFormats
{
    public interface IFileFormat
    {
        string Name { get; }
        IReadOnlyList<string> AutodetectFileNameRegexes { get; }
        int Ordinal { get; }
        bool AutodetectFromContents(string[] firstFewLines);
        IEnumerable<RawEvent> Read(IEnumerable<string> lines);
    }
}