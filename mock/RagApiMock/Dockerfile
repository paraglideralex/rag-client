FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

COPY RagApiMock.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /out .

ENV ASPNETCORE_HTTP_PORTS=7070
EXPOSE 7070

ENTRYPOINT ["dotnet", "RagApiMock.dll"]
