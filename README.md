# ShopNet — .NET Microservices

![CI](https://github.com/miteshdekate93/shopnet-microservices/actions/workflows/ci.yml/badge.svg)
![.NET 8](https://img.shields.io/badge/.NET-8-purple)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED)
![Ocelot](https://img.shields.io/badge/Gateway-Ocelot-orange)

An e-commerce backend built as microservices — three independent .NET 8 services communicating through a single API gateway. Each service has its own database. Built to demonstrate microservices architecture patterns.

## How It's Structured

```
Client
  └── API Gateway (Ocelot) :5000
        ├── /catalog/**  → Catalog Service :5001  (products)
        ├── /orders/**   → Order Service   :5002  (orders)
        └── /auth/**     → Identity Service:5003  (login/register)

Each service has its own PostgreSQL database.
```

## Services

| Service | What it does | Port |
|---------|-------------|------|
| **Gateway** | Routes requests to the right service (Ocelot) | 5000 |
| **Catalog** | Manages products and categories | 5001 |
| **Orders** | Creates and tracks orders | 5002 |
| **Identity** | User registration, login, JWT tokens | 5003 |

## Tech Stack

- .NET 8 Web API (all services)
- Ocelot API Gateway
- PostgreSQL — one database per service
- Entity Framework Core
- JWT Authentication + BCrypt
- Docker Compose
- GitHub Actions CI

## Run It

```bash
git clone https://github.com/miteshdekate93/shopnet-microservices.git
cd shopnet-microservices
docker-compose up --build
```

Gateway available at http://localhost:5000. Individual Swagger UIs at :5001/swagger, :5002/swagger, :5003/swagger.

## Project Structure

```
shopnet-microservices/
├── gateway/                  Ocelot API Gateway
├── services/
│   ├── catalog-service/      Products + Categories API
│   ├── order-service/        Orders API
│   └── identity-service/     Auth API (JWT)
└── docker-compose.yml
```
