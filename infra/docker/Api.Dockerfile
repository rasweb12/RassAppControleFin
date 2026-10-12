
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/RassAppControleFin.Api/RassAppControleFin.Api.csproj --locked-mode
RUN dotnet publish src/RassAppControleFin.Api/RassAppControleFin.Api.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

USER root

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app/publish .

USER $APP_UID

EXPOSE 10000
ENTRYPOINT ["dotnet", "RassAppControleFin.Api.dll"]

