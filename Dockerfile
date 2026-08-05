# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY ./src ./src
COPY ./tests ./tests
COPY DnsForwarder.sln .

RUN dotnet restore
RUN dotnet publish src/DnsForwarder/DnsForwarder.csproj -c Release -o /out

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

COPY --from=build /out .

# Expose DNS UDP port
EXPOSE 53/udp

# Run DNS forwarder
ENTRYPOINT ["dotnet", "DnsForwarder.dll"]
