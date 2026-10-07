# agentic-engineering-experiment

End-to-end experimentation with agentic engineering using .NET Aspire, GitHub Copilot CLI, Aspire Skills, and HVE Core.

## Purpose

This repository is an end-to-end experiment in building a distributed .NET application using an agentic engineering workflow.

The goal is to explore how repository context, engineering instructions, agent skills, structured workflows, runtime feedback, testing, and observability can work together to make AI-assisted software engineering more reliable and repeatable.

The experiment will be used to inform the design of a hands-on Agentic Engineering workshop.

## The Application

The application built throughout the experiment is **SupportHub**, a distributed customer-support platform.

SupportHub starts intentionally small and evolves through realistic business requirements. Infrastructure and additional services are introduced only when application requirements justify them.

The current solution contains:

- `SupportHub.Web` — Blazor frontend for support staff.
- `SupportHub.Customers.Api` — customer management.
- `SupportHub.Tickets.Api` — ticket management.
- `SupportHub.AppHost` — .NET Aspire application composition and orchestration.
- `SupportHub.ServiceDefaults` — shared service discovery, resilience, health checks, and OpenTelemetry configuration.

## Planned Features

### Customer Management

- Create, view, and update customers.
- Customer contact information.
- Customer support tier/profile.
- Customer and ticket relationships.

### Ticket Management

- Create, view, and update support tickets.
- Priority and status.
- Customer association.
- Ticket assignment.
- Ticket lifecycle management.

### Ticket Activity and Audit

- Record ticket creation and changes.
- Status and assignment history.
- Comments and other activity.
- Asynchronous activity processing.
- Maintain an auditable ticket timeline.

### Ticket Attachments

- Upload attachments.
- Download attachments.
- Associate attachment metadata with tickets.
- Store files outside transactional ticket data.

### SLA Management

- Define SLA rules based on priority and customer support tier.
- Calculate SLA deadlines.
- Identify tickets approaching SLA deadlines.
- Detect SLA breaches.
- Process SLA state in the background.
- Support on-demand SLA scanning and administrative operations.

### Support Dashboard

- Open tickets.
- Urgent tickets.
- Tickets at risk of SLA breach.
- Recent ticket activity.
- Customer and ticket navigation.

### Observability and Diagnostics

- Resource health.
- Structured logging.
- Distributed tracing.
- Metrics.
- Cross-service correlation.
- Runtime diagnostics through the Aspire Dashboard.

## Target Architecture

The complete architecture is not created upfront. Infrastructure is introduced as requirements emerge.

| Capability | Technology |
| --- | --- |
| Transactional data | PostgreSQL |
| Activity/audit/document data | Cosmos DB |
| Attachments | Azure Blob Storage |
| Asynchronous messaging | Azure Service Bus |
| Caching | Redis |
| Background processing | .NET Worker Service |
| Scheduled/admin/SLA jobs | .NET Console Application |
| Frontend | Blazor |
| APIs | ASP.NET Core |
| Orchestration | .NET Aspire |
| Observability | OpenTelemetry + Aspire Dashboard |

## Agentic Engineering Workflow

Substantial changes are developed using a Research → Plan → Implement → Review workflow.

### Research

Understand the requirement, repository, existing architecture, service relationships, and runtime topology before making changes.

### Plan

Design the implementation, identify affected services and determine whether new infrastructure is justified.

### Implement

Implement the approved plan while using Aspire for orchestration, runtime feedback, diagnostics, and verification.

### Review

Review the implementation using code, tests, resource health, logs, traces, and end-to-end application behavior.

HVE Core provides the structured engineering workflow, GitHub Copilot CLI provides the agent interaction surface, and .NET Aspire provides application context, orchestration, observability, and runtime feedback.

## Evolution Through Requirements

A key part of the experiment is allowing architecture to emerge from application requirements rather than constructing the final distributed system upfront.

Examples:

- Persistent customers and tickets → PostgreSQL.
- Ticket attachments → Azure Blob Storage.
- Ticket lifecycle events → Azure Service Bus.
- Activity and audit history → Cosmos DB.
- Asynchronous event processing → Worker Service.
- Read-heavy scenarios → Redis.
- SLA and administrative operations → Worker and Console applications.

Each requirement therefore exercises both feature development and architectural evolution.