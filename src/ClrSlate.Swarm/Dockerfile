# Use the .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ../../ClrSlate.Swarm.sln .
COPY . .

# Restore dependencies for the entire solution
RUN dotnet restore "ClrSlate.Swarm.sln"

# Build and publish the application
WORKDIR "/src/src/ClrSlate.Swarm"
RUN dotnet publish "ClrSlate.Swarm.csproj" -c Release -o /app/publish

# Use the ASP.NET runtime image to run the application
FROM mcr.microsoft.com/dotnet/aspnet:9.0

# Install dependencies for MCP servers
RUN apt-get update && \
    apt-get install -y curl python3 python3-pip nodejs npm && \
    curl -LsSf https://astral.sh/uv/install.sh | sh

# Add uv to the PATH
ENV PATH="/root/.local/bin:${PATH}"

WORKDIR /app

COPY --from=build /app/publish .

# Expose the port the application runs on
EXPOSE 8080

# Set the entrypoint to run the application
ENTRYPOINT ["dotnet", "ClrSlate.Swarm.dll"]
