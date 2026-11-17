# Documentation

Find the page for your question.

| I want to... | Read |
|---|---|
| Understand how the app is built | [architecture.md](architecture.md) |
| Understand the servers, deploys and backups | [infrastructure.md](infrastructure.md) |
| Run the code on my machine | [development.md](development.md) |
| Know why we chose something | [adrs/](adrs/) |
| Do an operation step by step | [runbooks/](runbooks/) |
| Change the server setup (Ansible, scripts) | [infrastructure/README.md](../infrastructure/README.md) |

## Folders

| Folder | Contains |
|---|---|
| `adrs/` | Architecture Decision Records: one decision per file, with its context and its cost. |
| `runbooks/` | Tested procedures. Each one ends with "Done when". |
| `diagrams/` | draw.io source files. |

## Rules for these docs

- One page answers one question. The first line says which.
- Tables and diagrams before long text.
- Link to the code; do not copy it.
- Runbooks only describe steps that were tested.
- No secrets, IPs or open security issues here. This repository is public.
