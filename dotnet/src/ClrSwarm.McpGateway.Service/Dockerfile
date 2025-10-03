# syntax=docker/dockerfile:1

# Build stage: restore and publish the ASP.NET Core service
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution configuration and project files for restore
COPY dotnet/Directory.Packages.props ./dotnet/
COPY dotnet/ClrSwarm.slnx ./dotnet/
COPY dotnet/src/ClrSwarm.McpGateway.Service/ClrSwarm.McpGateway.Service.csproj ./dotnet/src/ClrSwarm.McpGateway.Service/
COPY dotnet/src/ClrSwarm.McpGateway.Management/ClrSwarm.McpGateway.Management.csproj ./dotnet/src/ClrSwarm.McpGateway.Management/
COPY dotnet/src/ClrSwarm.McpGateway.ServiceDefaults/ClrSwarm.McpGateway.ServiceDefaults.csproj ./dotnet/src/ClrSwarm.McpGateway.ServiceDefaults/

RUN dotnet restore dotnet/src/ClrSwarm.McpGateway.Service/ClrSwarm.McpGateway.Service.csproj

# Copy remaining source and publish
COPY dotnet/ ./dotnet/
RUN dotnet publish dotnet/src/ClrSwarm.McpGateway.Service/ClrSwarm.McpGateway.Service.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage: run the published output
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Set ASP.NET Core to listen on port 8080 inside the container
ENV ASPNETCORE_URLS=http://+:8080

# Expose the HTTP port used by the service
EXPOSE 8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ClrSwarm.McpGateway.Service.dll"]
