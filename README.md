# Flat log file to Seq importer

A tool for parsing flat log files and importing them into Seq.

Features:
- Auto-detects file type
- Parses the log entries down for a somewhat structured logging experience (ie. log level, time, thread) including certain common/useful messages so that it uses a common message template.
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
1. [Install](https://nuke.build/docs/introduction/) and run `nuke` or run the `Default` target from your IDE (eg. Rider with the NUKE plugin)
2. Running directly:
   1. See examples above
3. Running in Docker:
   1. A docker image with the tag `seq-flat-file-import` is created from the Nuke build
   2. You will need to run a Seq instance in a separate container
   3. Example of importing all log files in your current working directory:
      1. Windows: `docker run --rm -v ${PWD}:/logs seq-flat-file-import --server=http://host.docker.internal:5341 ./logs`
      2. Linux/Mac: `docker run --rm -v $(pwd):/logs seq-flat-file-import --server=http://host.docker.internal:5341 ./logs`

`.\scripts\reset-seq.cmd` can be used to reset your local Seq instance, however this does not work with Seq instances in a container.