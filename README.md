# Flat log file to Seq importer

A quick and dirty log file parser and export to Seq, if logging to a friendly format is not available

Features:
- Autodetects file type
- Parses the log entries down for a somewhat structured logging experience (ie log level, time, thread) including certain common/useful messages so that it uses a common message template.
- Supports:
    - Octopus Server Log
    - Web Log
    - IIS Log
    - Octopus Deployment log exported from the web UI
- It can easily be extended to other log formats or parsing extra log lines

## Example

`SeqFlatFileImport.exe OctopusServer.txt OctopusServer.0.txt --batch MyDebuggingTask`
