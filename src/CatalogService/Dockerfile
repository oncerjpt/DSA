FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DSA.slnx ./
COPY src/Shared.Contracts/Shared.Contracts.csproj src/Shared.Contracts/
COPY src/CatalogService/CatalogService.csproj src/CatalogService/
RUN dotnet restore src/CatalogService/CatalogService.csproj

COPY src/Shared.Contracts/ src/Shared.Contracts/
COPY src/CatalogService/ src/CatalogService/
RUN dotnet publish src/CatalogService/CatalogService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet","CatalogService.dll"]

