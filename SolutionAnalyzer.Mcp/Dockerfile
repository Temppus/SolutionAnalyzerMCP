FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only project file and NuGet config first (for layer caching)
COPY SolutionAnalyzer.Mcp.csproj ./
COPY nuget.config ./
RUN dotnet restore --configfile nuget.config

# Copy the rest of the app
COPY . ./
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "SolutionAnalyzer.Mcp.dll"]
