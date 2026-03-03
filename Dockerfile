# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/ .
RUN dotnet restore ScribbleOpeAIAnalysis/ScribbleOpeAIAnalysis.csproj
RUN dotnet publish ScribbleOpeAIAnalysis/ScribbleOpeAIAnalysis.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ScribbleOpeAIAnalysis.dll"]
