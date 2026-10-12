FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/RassAppControleFin.Admin.Web/RassAppControleFin.Admin.Web.csproj --locked-mode
RUN dotnet publish src/RassAppControleFin.Admin.Web/RassAppControleFin.Admin.Web.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 10000
ENTRYPOINT ["dotnet", "RassAppControleFin.Admin.Web.dll"]
