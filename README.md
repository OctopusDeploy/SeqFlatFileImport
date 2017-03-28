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

`SeqFlatFileImport.exe c:\logs --batch MyDebuggingTask`

## Getting Started
1. Run `build.cmd`
2. The `artifacts` directory will now contain the standalone exe and a zip thereof
3. Use `.\scripts\reset-seq.cmd` for a quick way to clean out and reset your local Seq instance