# .NET 8 Microservices with Docker Compose & Ocelot API Gateway

[![CI](https://github.com/miteshdekate93/dotnet-microservices-docker/actions/workflows/ci.yml/badge.svg)](https://github.com/miteshdekate93/dotnet-microservices-docker/actions/workflows/ci.yml)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker&logoColor=white)](https://docs.docker.com/compose/)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Microservices](https://img.shields.io/badge/Architecture-Microservices-orange)](https://microservices.io/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE)

Production-ready microservices architecture demo for an e-commerce backend built with .NET 8, Ocelot API Gateway, Docker Compose, and PostgreSQL.

---

## Architecture

```
Client → [Ocelot API Gateway :5000]
               ├── /catalog/** → Catalog Service :5001
               ├── /orders/**  → Order Service   :5002
               └── /auth/**    → Identity Service :5003

Each service → its own isolated PostgreSQL database
```

---

## Services

| Service           | Port | Responsibility                              |
|-------------------|------|---------------------------------------------|
| API Gateway       | 5000 | Ocelot reverse proxy & request routing      |
| Catalog Service   | 5001 | Product & category management (CRUD)        |
| Order Service     | 5002 | Order creation, tracking & status updates   |
| Identity Service  | 5003 | User registration, login & JWT issuance     |
| catalog-db        | —    | PostgreSQL 16 for catalog data              |
| order-db          | —    | PostgreSQL 16 for order data                |
| identity-db       | —    | PostgreSQL 16 for user/auth data            |

---

## Tech Stack

| Layer              | Technology                       |
|--------------------|----------------------------------|
| Runtime            | .NET 8 / ASP.NET Core 8          |
| API Gateway        | Ocelot 23.x                      |
| ORM                | Entity Framework Core 8 + Npgsql |
| Authentication     | JWT Bearer (System.IdentityModel) |
| Password Hashing   | BCrypt.Net-Next                  |
| Database           | PostgreSQL 16 (Alpine)           |
| Containerisation   | Docker + Docker Compose 3.9      |
| API Docs           | Swashbuckle / Swagger UI         |
| CI                 | GitHub Actions (matrix build)    |

---

## Getting Started

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (includes Compose)
- Git

### Run Everything

```bash
git clone https://github.com/miteshdekate93/dotnet-microservices-docker.git
cd dotnet-microservices-docker
docker-compose up --build
```

All services will start. The gateway is available at **http://localhost:5000**.

### Individual Service Swagger UIs

| Service         | URL                              |
|-----------------|----------------------------------|
| Catalog Service | http://localhost:5001/swagger    |
| Order Service   | http://localhost:5002/swagger    |
| Identity Service| http://localhost:5003/swagger    |

---

## Health Check Endpoints

Each service exposes a `/health` endpoint:

```
GET http://localhost:5001/health   → Catalog Service
GET http://localhost:5002/health   → Order Service
GET http://localhost:5003/health   → Identity Service
```

---

## Environment Variables

| Variable                                    | Service          | Description                        |
|---------------------------------------------|------------------|------------------------------------|
| `ConnectionStrings__DefaultConnection`      | All services     | PostgreSQL connection string        |
| `JwtSettings__Secret`                       | Identity Service | JWT signing secret (min 32 chars)  |
| `JwtSettings__ExpiryMinutes`                | Identity Service | Token expiry in minutes            |

---

## Project Structure

```
dotnet-microservices-docker/
├── docker-compose.yml
├── gateway/
│   ├── Gateway.csproj
│   ├── Program.cs
│   ├── ocelot.json
│   └── Dockerfile
├── services/
│   ├── catalog-service/
│   │   ├── CatalogService.csproj
│   │   ├── Program.cs
│   │   ├── Models/
│   │   │   ├── Product.cs
│   │   │   └── Category.cs
│   │   ├── Data/
│   │   │   └── CatalogDbContext.cs
│   │   └── Dockerfile
│   ├── order-service/
│   │   ├── OrderService.csproj
│   │   ├── Program.cs
│   │   ├── Models/
│   │   │   ├── Order.cs
│   │   │   └── OrderItem.cs
│   │   ├── Data/
│   │   │   └── OrderDbContext.cs
│   │   └── Dockerfile
│   └── identity-service/
│       ├── IdentityService.csproj
│       ├── Program.cs
│       ├── Models/
│       │   └── User.cs
│       ├── Services/
│       │   └── TokenService.cs
│       ├── Data/
│       │   └── IdentityDbContext.cs
│       └── Dockerfile
└── .github/
    └── workflows/
        └── ci.yml
```

---

## License

This project is licensed under the [MIT License](./LICENSE).
