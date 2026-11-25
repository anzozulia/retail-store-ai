FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/RetailStore.Web/RetailStore.Web.csproj RetailStore.Web/
RUN dotnet restore RetailStore.Web/RetailStore.Web.csproj
COPY src/RetailStore.Web/ RetailStore.Web/
RUN dotnet publish RetailStore.Web/RetailStore.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R $APP_UID /home/app/.aspnet
USER $APP_UID
ENTRYPOINT ["dotnet", "RetailStore.Web.dll"]
