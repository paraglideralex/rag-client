# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY RagClient.Api/RagClient.Api.csproj ./
RUN dotnet restore

COPY RagClient.Api/ ./
RUN dotnet publish -c Release -o /out --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /out .

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "RagClient.Api.dll"]
