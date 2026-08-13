# FinTrack Docker Execution Guide

This folder contains containerization configurations for both architectural phases of FinTrack.

## 🚀 Phase 1: Single-Process Deployment (Initial MVP)

Runs `FinTrack.Api` with in-process modular monolith modules, in-memory event dispatches with MongoDB Outbox, and local MongoDB. No external message broker required.

### Run Phase 1 Locally:

```bash
docker compose -f docker/phase1/docker-compose.yml up --build -d
```

- **API Endpoint:** http://localhost:5000
- **MongoDB:** mongodb://localhost:27017

---

## ⚡ Phase 2: Distributed Scale-Out Deployment (After-Load Phase)

Splits runtime into `FinTrack.Api` (HTTP edge with API Controllers) and `FinTrack.Host` (MassTransit background consumers/saga orchestrators), connected via **RabbitMQ** and **MongoDB**.

### Run Phase 2 Locally:

```bash
docker compose -f docker/phase2/docker-compose.yml up --build -d
```

- **API Endpoint:** http://localhost:5000
- **RabbitMQ Management Dashboard:** http://localhost:15672 (Credentials: `guest` / `guest`)
- **MongoDB:** mongodb://localhost:27017
