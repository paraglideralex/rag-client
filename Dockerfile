# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# Note: change to sdk:9.0 if .NET 10 SDK is not yet available
WORKDIR /app

COPY src/RagClient.Api/RagClient.Api.csproj ./
RUN dotnet restore

COPY src/RagClient.Api/ ./
RUN dotnet publish -c Release -o /out --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
# Note: change to aspnet:9.0 if .NET 10 is not yet available
WORKDIR /app

COPY --from=build /out .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RagClient.Api.dll"]
