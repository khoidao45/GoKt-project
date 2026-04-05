🚀 Gokt — Ride Hailing Backend (Production-Ready)
📌 Overview

Gokt is a production-grade ride-hailing backend system inspired by Grab/Uber architecture, built with:

Clean Architecture
Event-driven design (Kafka)
Outbox Pattern (reliable messaging)
Redis (real-time + rate limiting)
SignalR (real-time communication)
Background Workers
🧱 Architecture
API Layer (Gokt)
    ↓
Application Layer (Commands, Handlers, Interfaces)
    ↓
Domain Layer (Entities, Enums, Business Rules)
    ↓
Infrastructure Layer (EF Core, Kafka, Redis, Outbox, Workers)
⚙️ Tech Stack
.NET 8 / ASP.NET Core
PostgreSQL
Redis
Kafka (Confluent.Kafka)
SignalR
MediatR
Serilog
Docker
🔥 Key Features
🚗 Ride Request Flow
User creates ride request
Stored in DB
Event written to Outbox
Published to Kafka
MatchingWorker assigns driver
📦 Outbox Pattern (Core Reliability)

Ensures:

No event loss
Atomic DB + event write
Retry on failure
Eventual consistency
🔁 Outbox Processing
Background worker polls pending events
Publishes to Kafka
Handles retries
Marks events as:
Pending
Processed
Failed
☠️ Dead Letter Queue (DLQ)
Failed events are sent to DLQ topic
Enables debugging & recovery
🔄 Replay Mechanism
Admin can replay failed events
Resets event state:
Failed → Pending
Worker re-processes automatically
🧠 Matching System
Auto matching (scoring)
Driver code matching (manual override)
Cooldown system (Redis)
⚡ Real-time Updates
SignalR Hub
Redis backplane
Push updates to:
Users
Drivers
📁 Project Structure
src/
  Gokt.Domain/
    Entities/
    Enums/

  Gokt.Application/
    Commands/
    Interfaces/
    Events/

  Gokt.Infrastructure/
    Persistence/
    Messaging/
    Repositories/
    BackgroundServices/

  Gokt.MatchingWorker/
    Kafka consumers
    Matching logic

Gokt/
  Controllers/
  Program.cs
🗄️ Database
OutboxEvents Table
Column	Description
Id	Event ID
Type	Event type
Payload	JSON data
Status	Pending / Processed / Failed
RetryCount	Retry attempts
CreatedAt	Created time
ProcessedAt	Processed time
LastError	Error message
🔄 Event Flow
Client → API → DB (Ride + Outbox)
                     ↓
             OutboxProcessor
                     ↓
                 Kafka
                     ↓
            MatchingWorker
                     ↓
              Driver assigned
🚀 Getting Started
1. Run Infrastructure
docker-compose up -d
2. Apply Migration
dotnet ef database update
3. Run Services
dotnet run --project Gokt
dotnet run --project Gokt.MatchingWorker
🧪 Test Scenarios
✅ Kafka Down
Create ride request
Event saved in Outbox (Pending)
✅ Kafka Recovery
Worker retries
Event published
Status → Processed
❌ Failure Case
Retry > 5
Status → Failed
Event sent to DLQ
🔁 Replay
POST /outbox/replay/{id}
📊 Production Considerations
✅ Implemented
Outbox Pattern
Retry mechanism
DLQ
Idempotent producer
SKIP LOCKED concurrency
⚠️ Recommended Next
Inbox Pattern (consumer deduplication)
Metrics (Prometheus / Grafana)
Outbox cleanup job
Event versioning
Saga orchestration
💡 Design Principles
Event-driven architecture
Eventually consistent system
Resilient to failure
Scalable horizontally
Clean separation of concerns
🎯 Status

✅ Production-ready (MVP level)
🚀 Ready for scaling & advanced features

👨‍💻 Author

Built as a learning + production-grade system for mastering:

Distributed systems
Event-driven architecture
Real-time backend design