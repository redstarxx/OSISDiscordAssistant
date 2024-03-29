# Build it
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build

WORKDIR /App
COPY . ./
RUN dotnet restore ./src/OSISDiscordAssistant.csproj

RUN dotnet publish ./src/OSISDiscordAssistant.csproj -c Release -o out

# Run it
FROM mcr.microsoft.com/dotnet/runtime:6.0-alpine

# Install tzdata to access the western Indonesian timezone
ARG DEBIAN_FRONTEND=noninteractive
ENV TZ=Asia/Jakarta
RUN apk add --update tzdata

# Install cultures
RUN apk add --no-cache icu-libs

# Disable the invariant mode (set in base image)
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

WORKDIR /App
COPY --from=build /App/out .

RUN chmod +x ./OSISDiscordAssistant

CMD ["./OSISDiscordAssistant"]
