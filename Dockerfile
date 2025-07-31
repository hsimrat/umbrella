# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["TechScriptAid.API/TechScriptAid.API.csproj", "TechScriptAid.API/"]
COPY ["TechScriptAid.Core/TechScriptAid.Core.csproj", "TechScriptAid.Core/"]
COPY ["TechScriptAid.Infrastructure/TechScriptAid.Infrastructure.csproj", "TechScriptAid.Infrastructure/"]
COPY ["TechScriptAid.AI/TechScriptAid.AI.csproj", "TechScriptAid.AI/"]

RUN dotnet restore "TechScriptAid.API/TechScriptAid.API.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/TechScriptAid.API"
RUN dotnet build "TechScriptAid.API.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "TechScriptAid.API.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TechScriptAid.API.dll"]