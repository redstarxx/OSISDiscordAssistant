FROM mcr.microsoft.com/dotnet/aspnet:5.0

COPY src/bin/Release/netcoreapp5.0/publish/ App/
WORKDIR /App
ENTRYPOINT ["dotnet", "discordbot.dll"]