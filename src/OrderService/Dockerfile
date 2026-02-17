FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DSA.slnx ./
COPY src/Shared.Contracts/Shared.Contracts.csproj src/Shared.Contracts/
COPY src/OrderService/OrderService.csproj src/OrderService/
RUN dotnet restore src/OrderService/OrderService.csproj

COPY src/Shared.Contracts/ src/Shared.Contracts/
COPY src/OrderService/ src/OrderService/
RUN dotnet publish src/OrderService/OrderService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet","OrderService.dll"]

