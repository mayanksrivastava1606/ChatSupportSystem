# Chat Support System

A .NET 8 Web API that simulates a real-time customer support chat platform with intelligent agent assignment, shift management, queue capacity control, and overflow handling.

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Business Rules](#business-rules)
  - [Shifts](#shifts)
  - [Agent Capacity](#agent-capacity)
  - [Queue Limits](#queue-limits)
  - [Overflow](#overflow)
  - [Chat Assignment](#chat-assignment)
  - [Session Monitoring](#session-monitoring)
- [Team Configuration](#team-configuration)
- [API Endpoints](#api-endpoints)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Running Tests](#running-tests)

## Overview

When a user initiates a support request, the system creates a chat session and places it in a FIFO queue. A background monitor continuously watches the queue, assigns chats to available agents using a round-robin strategy (preferring junior agents first), and marks sessions inactive when clients stop polling.

### Key Features

- **FIFO chat queue** with thread-safe concurrent operations
- **3-shift rotation** (Day, Afternoon, Night) with automatic agent activation
- **Seniority-based capacity** — each agent's concurrency is derived from their seniority multiplier
- **Overflow team** — activates during office hours when the primary queue is full
- **Round-robin assignment** — prefers juniors first, keeping senior agents free to assist
- **Polling-based liveness** — sessions are marked inactive after 3 missed 1-second polls
- **Real-time status endpoint** for monitoring active agents and queue state

## Architecture

| Component | Responsibility |
|---|---|
| `ChatController` | API surface — create sessions, poll, view status |
| `ChatCoordinator` | Core orchestrator — capacity checks, overflow activation, session lifecycle |
| `ChatQueue` | Thread-safe FIFO queue backed by `ConcurrentQueue` and `ConcurrentDictionary` |
| `ChatAssignmentService` | Round-robin assignment logic, junior-first preference |
| `ShiftManager` | Determines active shift, office hours, and agent availability |
| `QueueMonitorService` | Background service — detects inactive sessions, triggers pending assignments |
| `TeamConfigurationService` | Seeds all agent/team data |

## Business Rules

### Shifts

Agents work **3 shifts of 8 hours each** (UTC):

| Shift | Hours (UTC) | Team |
|---|---|---|
| Day | 06:00 – 14:00 | Team A |
| Afternoon | 14:00 – 22:00 | Team B |
| Night | 22:00 – 06:00 | Team C |

When a shift ends, the agent **finishes current chats** but is **not assigned new ones**.

### Agent Capacity

Maximum concurrency per agent is **10**, multiplied by a seniority efficiency multiplier (rounded down):

| Seniority | Multiplier | Max Concurrent Chats |
|---|---|---|
| Junior | 0.4 | 4 |
| Mid-Level | 0.6 | 6 |
| Senior | 0.8 | 8 |
| Team Lead | 0.5 | 5 |

**Team capacity** = sum of each agent's max concurrent chats.

### Queue Limits

- **Maximum queue length** = team capacity × **1.5** (rounded down)
- Once the queue is full and it is **not** office hours, the chat is **refused** (HTTP 503)
- Once the queue is full and it **is** office hours, the overflow team activates

### Overflow

- The overflow team consists of **6 agents**, all treated as **Junior** (multiplier 0.4)
- Overflow is **only available during office hours** (06:00–22:00 UTC)
- Overflow adds its own capacity × 1.5 to the total allowed queue length
- Once **both** the primary and overflow queues are full, the chat is refused

### Chat Assignment

Chats are assigned using **round-robin within seniority tiers**, prioritized in this order:

1. **Junior** agents first
2. **Mid-Level**
3. **Senior**
4. **Team Lead**

This ensures higher-seniority agents remain available to assist lower-seniority agents.

**Examples:**

| Team Composition | Incoming Chats | Assignment |
|---|---|---|
| 1 Senior (cap 8), 1 Junior (cap 4) | 5 | 4 → Junior, 1 → Senior |
| 2 Junior (cap 4 each), 1 Mid-Level (cap 6) | 6 | 3 → Junior 1, 3 → Junior 2, 0 → Mid |

### Session Monitoring

- Clients must **poll every 1 second** via `POST /api/chat/{id}/poll`
- A background service checks sessions every second
- After **3 consecutive missed polls**, the session is marked **Inactive** and the agent's slot is freed
- A successful poll **resets** the missed poll counter to 0

## Team Configuration

| Team | Agents | Shift | Capacity | Max Queue |
|---|---|---|---|---|
| **Team A** | 1 Team Lead (5) + 2 Mid-Level (12) + 1 Junior (4) | Day | **21** | **31** |
| **Team B** | 1 Senior (8) + 1 Mid-Level (6) + 2 Junior (8) | Afternoon | **22** | **33** |
| **Team C** | 2 Mid-Level (12) | Night | **12** | **18** |
| **Overflow** | 6 Junior (24) | Office hours | **24** | **+36** |

## API Endpoints

### Create Chat Session

- **Endpoint:** `/api/chat`
- **Method:** `POST`
- **Request Body:** `{ "clientName": "string", "topic": "string" }`
- **Response (200 OK):**

    ```json
    {
      "id": "string",       // Unique identifier for the chat session
      "status": "string",   // Current status of the session (e.g., Active, Inactive)
    }
    ```
- **Response (503 Service Unavailable):**

    ```json
    {
      // No response body
    }
    ```

### Poll Session

- **Endpoint:** `/api/chat/{id}/poll`
- **Method:** `POST`
- **Response (200 OK):**

    ```json
    {
      "id": "string",       // Unique identifier for the chat session
      "status": "string",   // Current status of the session (e.g., Active, Inactive)
    }
    ```
- **Response (404 Not Found):** returned when the session is inactive or unknown.

### System Status

- **Endpoint:** `/api/status`
- **Method:** `GET`
- **Response (200 OK):**

    ```json
    {
      "activeAgents": 10,   // Number of agents currently active
      "totalChats": 50,     // Total number of chats in the system
      "queueLength": 5       // Current length of the chat queue
    }
    ```

## Project Structure

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (17.8+) or the .NET CLI

### Run the Application

The API will be available at `https://localhost:<port>`. Navigate to `/swagger` for the interactive Swagger UI.

### Docker

### Running Tests

### Visual Studio

Open **Test > Test Explorer** (`Ctrl+E, T`) then click **Run All** (`Ctrl+R, A`)

### CLI

````````

Filter to a specific test class:

````````

### Test Coverage

| Test Class | Coverage Area |
|---|---|
| `AgentTests` | Seniority multipliers, max concurrency, slot management, shift-over behavior |
| `ChatQueueTests` | FIFO ordering, enqueue/dequeue, peek, status counting |
| `ShiftManagerTests` | Shift resolution, office hours detection, agent status updates |
| `ChatAssignmentServiceTests` | Junior-first preference, round-robin, spec scenario validation |
| `ChatCoordinatorTests` | Team capacity (A=21, B=22, C=12), overflow (24), queue limits, polling |
| `QueueCapacityTests` | Queue full → refusal, overflow activation |
| `SessionInactivityTests` | Missed poll detection, poll reset, agent slot cleanup |
| `TeamConfigurationServiceTests` | Team composition, agent counts, capacity math |
