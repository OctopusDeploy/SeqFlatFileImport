using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Seq.Api;
using SeqFlatFileImport.Core.FileFormats;

namespace SeqFlatFileImport.Core
{
    public class Importer
    {
        private readonly Action<string> _progressCallback;

        public static List<IFileFormat> DefaultFileFormats { get; } =
            typeof(Importer).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IFileFormat).IsAssignableFrom(t))
            .Select(t => (IFileFormat) Activator.CreateInstance(t))
            .ToList();

        private readonly ISeqEndpoint _seqEndpoint;
        private readonly IReadOnlyList<IFileFormat> _fileFormats;

        public Importer(string seqServer = SeqEndpoint.DefaultUri, string seqApiKey = null, IReadOnlyList<IFileFormat> fileFormats = null, Action<string> progressCallback = null, string batchId = null)
            : this(new SeqEndpoint(seqServer, seqApiKey, batchId), fileFormats, progressCallback)
        {
        }

        internal Importer(ISeqEndpoint seqEndpoint, IReadOnlyList<IFileFormat> fileFormats = null, Action<string> progressCallback = null)
        {
            _seqEndpoint = seqEndpoint;                                                                            
            _progressCallback = progressCallback ?? (s => { });
            _fileFormats = fileFormats ?? DefaultFileFormats.ToArray();
        }

        public IResult Import(string file, string formatName = null)
        {
            using (var fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var fileFormatResult = formatName == null ? AutoDetectFormat(fs, Path.GetFileName(file)) : FindFormatByName(formatName);
                return fileFormatResult.WasSuccessful ? Import(fs, fileFormatResult.Value) : fileFormatResult;
            }
        }

        public Result<IFileFormat> AutoDetectFormat(Stream stream, string filename)
        {
            foreach (var format in _fileFormats.OrderBy(f => f.Ordinal))
                foreach (var regex in format.AutodetectFileNameRegexes)
                    if (Regex.IsMatch(filename, $"^{regex}$"))
                        return format.AsSuccess();

            string[] firstFewLines;
            using (var reader = new Reader(stream))
                firstFewLines = reader.Enumerable().Take(10).ToArray();

            foreach (var format in _fileFormats.OrderBy(f => f.Ordinal))
                if (format.AutodetectFromContents(firstFewLines))
                    return format.AsSuccess();

            return Result.Failed<IFileFormat>("Could not autodetect file type");
        }

        public Result<IFileFormat> FindFormatByName(string formatName)
        {
            return _fileFormats.FirstOrNone(f => f.Name.Equals(formatName, StringComparison.CurrentCultureIgnoreCase))
                .ToResult($"Could not find the file format '{formatName}'");
        }

        public IResult Import(Stream stream, IFileFormat format)
        {
            var line = 0;
            using (var reader = new Reader(stream))
                foreach (var rawEvent in format.Read(reader.Enumerable()))
                {
                    _seqEndpoint.Write(rawEvent);
                    line++;
                    if (line%1000 == 0)
                        _progressCallback("Line " + line);
                }
            _progressCallback("Finished reading, waiting for data to finish sending");
            return _seqEndpoint.Flush();
        }
    }
}