# PayFlow 💸

PayFlow is a robust backend engine for a digital wallet and payment processing system. It is built using modern **Fintech engineering principles** to ensure high availability, strong consistency, and zero loss of funds, acting much like the core processing layer of systems like Venmo or PayPal.

## 🚀 Key Features

* **Strict Double-Entry Ledger**: Every transaction (Top-Ups, Transfers, Refunds) generates immutable ledger entries. Money is never created or destroyed out of thin air; every debit has a matching credit.
* **Idempotency Engine**: Uses SHA-256 payload hashing and Redis caching to guarantee that bad network connections or accidental duplicate clicks never result in double-charging a user.
* **Transactional Outbox Pattern**: Database transactions and domain events are decoupled. Fast API responses are guaranteed by handing off background tasks to a RabbitMQ message bus asynchronously.
* **Optimistic Concurrency**: Prevents race conditions right at the database row level to ensure accurate wallet balances under high concurrent load.

## 🛠 Tech Stack

Built natively in **.NET 8** and orchestrated entirely with Docker.

- **Core Framework**: .NET 8, C#, ASP.NET Core API
- **Architecture**: Domain-Driven Design (DDD), Clean Architecture, CQRS
- **Database**: Azure SQL Edge (SQL Server) + Entity Framework Core
- **Caching & Idempotency**: Redis
- **Message Broker**: RabbitMQ
- **Background Workers**: .NET Hosted Services (Worker Service)

## 🏗 Architecture Overview

The system is split into two primary running services:
1. `PayFlow.Api` - The fast, user-facing REST API that accepts payment requests, validates rules, updates the database, and publishes an outbox message.
2. `PayFlow.Worker` - A background daemon that polls the Outbox, publishes domain events over RabbitMQ, and handles heavy-lifting like Event Reconciliation and Notifications.

---

## 🏃 Running Locally

You do not need `.NET` installed on your machine to run this project. The entire architecture, including the API, Background Worker, and databases, are containerized.

### 1. Start the Stack
From the root of the project, run:
```bash
docker compose -f docker/docker-compose.yml up --build -d
```
This spins up:
- The PayFlow API
- The PayFlow Worker
- SQL Server Container
- Redis Container
- RabbitMQ Management Container

### 2. Access the Infrastructure
- **Swagger API Documentation:** `http://localhost:8080/swagger`
- **RabbitMQ Dashboard:** `http://localhost:15672` *(guest / guest)*
- **SQL Server Database:** `localhost:1433` *(sa / PayFlow@Dev123)*

---

## 🧪 Testing the API

You can test the entire flow using the attached `SWAGGER_TEST_GUIDE.md` which includes copy-paste commands to:
1. Create a wallet for a sender.
2. Create a wallet for a receiver.
3. Simulate a fake bank deposit (Top-Up) to your wallet.
4. Transfer money securely to the receiver.
5. Verify idempotency.

> **Note:** For quick testing, two dummy Users pre-exist in the Docker SQL container database under the IDs `00000000-0000-0000-0000-000000000001` and `00000000-0000-0000-0000-000000000002` to bypass Foreign Key constraints.

---

## 🧪 Running Unit Tests
*(Requires `.NET 8 SDK` installed locally, or run inside a docker container)*
```bash
dotnet test tests/PayFlow.UnitTests/ --verbosity normal
```
