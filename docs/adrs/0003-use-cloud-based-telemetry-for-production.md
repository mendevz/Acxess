## Status
Accepted

## Context
Acxess requires robust structured logging and observability to monitor system health and trace domain errors in production. However, our production VPS operates with strict hardware constraints (4 GB RAM). Self-hosting a centralized logging server like Seq on the same machine requires an additional 1-2 GB of dedicated memory. Allocating this RAM to a logging container would starve the SQL Server container (which requires maximum available memory for its Buffer Pool), significantly increasing the risk of Out-Of-Memory (OOM) crashes and system downtime during peak gym operations.

## Decision
We will externalize our production observability by routing logs to a cloud-based telemetry service (BetterStack). 
We will configure Serilog using the `BetterStack.Logs.Serilog` and `Serilog.Sinks.Async` packages. The asynchronous sink ensures that network latency or third-party outages do not block the main application threads. 
For local development, we will retain a self-hosted Seq Docker container to debug issues without consuming the external service's quota.

## Consequences
* **Positive:** Zero memory footprint on the production VPS for log aggregation. SQL Server retains maximum available RAM for performance. Logs remain highly available and accessible even if the production server crashes entirely.
* **Positive:** Separation of concerns. The production server is strictly dedicated to serving the application and database.
* **Negative:** Introduces a dependency on a third-party SaaS vendor. 
* **Negative:** We must monitor the monthly log volume to stay within the free tier limits (1 GB/month) or budget for a paid plan as the client base scales.