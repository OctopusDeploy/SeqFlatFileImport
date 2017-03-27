net stop Seq

rmdir "C:\ProgramData\Seq" /S /Q

net start Seq

start "" http://localhost:5341