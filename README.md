# Gokt — Ride Hailing Platform

A production-grade ride-hailing system inspired by Grab/Uber, built with Clean Architecture, event-driven design, and real-time communication.

**Live Demo:** [https://gokt.minhkhoidao.id.vn](https://gokt.minhkhoidao.id.vn) | **Jenkins:** [https://jenkins.minhkhoidao.id.vn](https://jenkins.minhkhoidao.id.vn)

---

## Demo Accounts

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@gokt.vn | Admin@123456 |
| Rider | Register a new account | — |
| Driver | Register via Admin panel | — |

> Admin can manage drivers, view trips, monitor live map, and replay failed events.

---

## Tech Stack

**Backend**
- .NET 8 / ASP.NET Core Web API
- PostgreSQL + Entity Framework Core 8
- Redis — caching, rate limiting, geolocation, SignalR backplane
- Apache Kafka — event streaming between API and MatchingWorker
- SignalR — real-time ride updates and driver tracking
- MediatR — CQRS pattern
- Serilog — structured logging
- SendGrid — email verification
- Google OAuth2 — social login
- Argon2 — password hashing
- Polly — retry policies

**Frontend**
- React 19 + TypeScript + Vite
- Leaflet — interactive maps
- SignalR client — real-time updates

**DevOps**
- Docker + Docker Compose
- Jenkins CI/CD — automated test → build → push → deploy pipeline
- Azure Container Registry (ACR)
- Azure VM (Ubuntu)
- Nginx — reverse proxy + SSL termination
- Let's Encrypt — HTTPS

---

## Architecture

```
Clean Architecture
  Gokt (API)
    ↓
  Gokt.Application  (Commands, Handlers — CQRS via MediatR)
    ↓
  Gokt.Domain       (Entities, Business Rules)
    ↓
  Gokt.Infrastructure (EF Core, Kafka, Redis, Outbox, Workers)

Gokt.MatchingWorker (Kafka consumer — runs 2 replicas in production)
```

---

## Key Features

**Ride Flow**
1. User creates ride request → saved to DB
2. Event written to Outbox atomically
3. OutboxProcessor publishes to Kafka
4. MatchingWorker consumes event → assigns nearest driver
5. SignalR pushes real-time updates to both user and driver

**Outbox Pattern**
- Guarantees no event loss even if Kafka is down
- Atomic DB + event write
- Retry with max 5 attempts → Dead Letter Queue on failure
- Admin can replay failed events via API

**Matching System**
- Auto matching by distance scoring
- Driver code matching (manual override)
- Redis-backed cooldown system

**Background Workers**
- `OutboxProcessor` — publishes pending events to Kafka
- `RideExpiryWorker` — auto-cancels expired ride requests
- `DriverDailyPayrollWorker` — calculates daily driver earnings
- `UnverifiedUserCleanupWorker` — removes unverified accounts

---

## CI/CD Pipeline

```
GitHub push
    ↓
Jenkins (on Azure VM)
    ├── dotnet test
    ├── npm ci + vite build
    ├── az login + acr login
    ├── docker build + push → ACR
    └── SSH deploy → docker compose up
```

---

## Project Structure

```
src/
  Gokt.Domain/          # Entities, Enums, Business Rules
  Gokt.Application/     # Commands, Queries, Handlers (CQRS)
  Gokt.Infrastructure/  # EF Core, Kafka, Redis, Workers
  Gokt.MatchingWorker/  # Kafka consumer + matching logic
  Gokt.Hubs/            # SignalR hub definitions

Gokt/                   # ASP.NET Core API entry point
  Controllers/
  Middleware/
  Program.cs

frontend/               # React + TypeScript
tests/
  Gokt.Domain.Tests/
  Gokt.Infrastructure.Tests/
```

---

## Local Development

```bash
# Start infrastructure
docker compose up -d

# Run API
dotnet run --project Gokt

# Run MatchingWorker
dotnet run --project src/Gokt.MatchingWorker

# Run Frontend
cd frontend && npm install && npm run dev
```

---

## Author

Đào Minh Khôi — Built as a learning + production-grade project for mastering distributed systems, event-driven architecture, and real-time backend design.
