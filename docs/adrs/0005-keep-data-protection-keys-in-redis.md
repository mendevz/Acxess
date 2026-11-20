# 5. Keep the Data Protection keys in Redis

## Status

Accepted (2026-11-18)

## Context

ASP.NET Core uses **Data Protection keys** to sign and encrypt some short-lived values. In Acxess they protect:

- the sign-in cookie (it lasts 30 days),
- the antiforgery tokens of the forms,
- Identity tokens (for example, password reset links).

No data in the database is encrypted with these keys.

Today the keys live in Redis (`PersistKeysToStackExchangeRedis`, application name `AcxessApp`). Redis has a volume and AOF persistence (`appendonly yes`), so the keys survive a restart. But **Redis has no backup**.

In the restore drills (2026-11-19), the new server started with an empty Redis. The app made new keys, and all users had to sign in again. No data was lost.

The question: should the keys move to SQL Server, so they come back with every database restore?

## Options

| | Redis (today) | SQL Server (`PersistKeysToDbContext`) |
|---|---|---|
| After a restore on a new server | Users sign in again | Sessions continue |
| Who can read the keys | Only someone with access to the running server | **Anyone with a database backup** |
| Extra code | None | A table, a migration and a `DbContext` change |

## Decision

**Keep the keys in Redis.**

The keys must not travel with the backups. A backup is copied many times: to R2, to the lab, to a second provider later. With the keys inside it, a person with **one backup file and its decryption key** could create valid sign-in cookies for any user, including the owners. The backup would then hold access to the app, not only data.

The cost of keeping them in Redis is small and safe: after a full recovery on a new server, users sign in again.

## Consequences

- **Positive:** A stolen backup gives data, not the ability to sign in as someone. The backups and the sign-in keys are two separate secrets.
- **Positive:** No code change.
- **Negative:** After a recovery on a new server, every user signs in again. This is written in the [new server runbook](../runbooks/new-server.md).
- **Negative:** The keys are not encrypted inside Redis. Redis is protected by a password, and its port is not published outside the Docker network.
- **Later:** If we run more than one app instance, all instances must read the same keys. Redis already allows this. Before that, we can add encryption of the keys with `ProtectKeysWith...`.
