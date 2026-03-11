# HMEye

Lightweight HMI template for Beckhoff TwinCAT, built with **Blazor Server** and **MudBlazor**.
Provides a simple, open alternative to proprietary HMI platforms by relying entirely on common .NET web technologies.

## Design Intent

- **Open Tech Stack** — Uses standard technologies (C#, Blazor, MudBlazor, TwinCAT ADS) instead of closed vendor-specific systems to implement a web based HMI.
- **Minimal Backend Configuration** — The backend remains generic. Custom programming per machine is primarily in blazor client pages.
- **Centralized Communication** — Predictable polling model with a single point of PLC communication and value caching, allowing for safe connection of multiple clients.
- **Extensible Architecture** — Integrates parallel containerized services (e.g. Grafana, Node-RED) via a built-in YARP reverse proxy, providing unified access control.

## Architecture Overview

- **Attribute-Driven Symbol Discovery**  
  The PLC dictates the HMI symbols used for interface. Variables are automatically discovered via custom attribute in PLC code, and are automatically type-mapped.

- **Polling Cache Service**  
  The backend polls the discovered symbols and keeps their most recent values in an in-memory cache.

- **Asynchronous Writes**  
  The UI does not need to talk directly to the PLC. Reads and Writes are via the cache and the cache communicates asyncronously with TwinCAT.

- **Client Pages**  
  The primary area of active development. Pages are responsible for UI layout, binding to cached variables, and implementing machine-specific logic.

- **Self-Contained Authentication**  
  Built-in role-based access control restricts access to specific UI pages, machine features, and proxied YARP services.

## Getting Started

*(To maybe once be added — e.g. prerequisites, Docker setup, PLC attribute example, ... )*

## License

MIT License — see the [LICENSE](LICENSE) file for details.
