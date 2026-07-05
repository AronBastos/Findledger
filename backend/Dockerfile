FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/ ./src/
RUN dotnet restore src/FinLedger.API/FinLedger.API.csproj
RUN dotnet publish src/FinLedger.API/FinLedger.API.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FinLedger.API.dll"]
