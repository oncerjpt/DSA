FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DSA.slnx ./
COPY src/Shared.Contracts/Shared.Contracts.csproj src/Shared.Contracts/
COPY src/PaymentService/PaymentService.csproj src/PaymentService/
RUN dotnet restore src/PaymentService/PaymentService.csproj

COPY src/Shared.Contracts/ src/Shared.Contracts/
COPY src/PaymentService/ src/PaymentService/
RUN dotnet publish src/PaymentService/PaymentService.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet","PaymentService.dll"]

