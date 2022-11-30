# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
COPY ./artifacts .
ENTRYPOINT ["dotnet", "SeqFlatFileImport.dll"]
