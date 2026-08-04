## Status
Accepted

## Context
Acxess previously relied on an unofficial WhatsApp API service (Evolution API) hosted via Docker (PostgreSQL, Redis, and Evolution API containers) to send subscription expiration reminders to gym administrators. 
Due to Meta's strict anti-spam enforcement, the unofficial WhatsApp Business account was blocked. Attempting to obtain official Meta WhatsApp Business API approval introduced unacceptable business delays (>2 weeks) during which critical expiration reminder notifications remained offline. 
Furthermore, self-hosting the Evolution API stack required 3 additional Docker containers and dedicated PostgreSQL + Redis instances, consuming valuable RAM and CPU resources on our production VPS (4 GB RAM constraint).

## Decision
We decided to replace the self-hosted Evolution API / WhatsApp notification infrastructure with **Twilio SMS**. 

Key technical implementation details:
1. Abstracted `IWhatsAppService` into `ISendRemindersService` across the shared domain layer.
2. Implemented `SmsService` backed by the official `Twilio` C# SDK.
3. Refactored `SendDailyExpirationRemindersHandler` to build concise single-segment SMS messages to minimize transactional costs.
4. Removed artificial "anti-ban" delays (`Task.Delay(humanDelayMs)`), enabling instant high-throughput dispatch.
5. Decommissioned and removed all 3 Evolution API Docker services (`evolution-api`, `evolution-postgres`, `evolution-redis`) and their associated volumes from `docker-compose.yml` and `docker-compose.prod.yml`.
6. Configured production deployment secrets via GitHub Actions secrets (`TWILIO_ACCOUNT_SID`, `TWILIO_AUTH_TOKEN`, `TWILIO_FROM_PHONE_NUMBER`).

## Consequences
* **Positive:** **High Reliability & Compliance:** 100% legal, official delivery mechanism with zero risk of account bans or unexpected service disruption.
* **Positive:** **Reduced Resource Footprint:** Freed up significant RAM/CPU and disk space on the production server by eliminating 3 Docker containers (Evolution API, PostgreSQL, Redis).
* **Positive:** **Performance:** High-throughput dispatch without needing human-like anti-ban delays.
* **Positive:** **Future Scalability:** Twilio provides a direct pathway to official WhatsApp Business API if approved by Meta in the future, requiring minimal code changes.
* **Negative:** Introduces a small pay-as-you-go transactional cost (~$0.0079 USD per SMS + ~$1.15 USD/month phone number rental).
