# Build it
FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build

WORKDIR /App
COPY . ./
RUN dotnet restore ./src/discordbot.csproj

RUN dotnet publish ./src/discordbot.csproj -c Release -o out

# Run it
FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine

WORKDIR /App
COPY --from=build /App/out .

RUN chmod +x ./discordbot

CMD ["./discordbot"]
