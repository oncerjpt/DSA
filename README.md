# DSA

## Microservices scaffold (.NET 10 / C# 14)

Build:

```bash
dotnet build DSA.slnx
```

Run via Docker Compose:

```bash
docker compose up --build
```

Services:
* CatalogService: http://localhost:8081 (OpenAPI: `/openapi/v1.json`, Health: `/health`)
* PaymentService: http://localhost:8082 (OpenAPI: `/openapi/v1.json`, Health: `/health`)
* OrderService: http://localhost:8083 (OpenAPI: `/openapi/v1.json`, Health: `/health`)
