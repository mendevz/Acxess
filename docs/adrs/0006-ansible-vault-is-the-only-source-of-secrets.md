# 6. Ansible Vault is the only source of secrets

## Status

Accepted (2026-11-26)

## Context

Production secrets lived in two places:

- **GitHub Secrets.** On every release, `deploy.yml` wrote the whole `.env` of the app on the server.
- **Ansible Vault.** The playbook wrote the same `.env`, plus the backup configuration.

Two writers for one file means that the last one wins. After a release, an Ansible run would write an old `APP_VERSION`. After an Ansible run, a release would write the GitHub values back. Also, GitHub Secrets cannot be read, so the only readable copy of the secrets was the file on the server.

## Decision

**Every value has one owner.**

| Value | Owner |
|---|---|
| Secrets and settings in `/opt/acxess/.env` | Ansible (Vault) |
| `APP_VERSION` | `deploy.yml`. It changes only that line. |
| `APP_VERSION` on an empty server | Ansible, from `app_version` in the variables. Only when the server has no version yet. |
| Backup configuration (`/opt/acxess-backup/.env`, rclone) | Ansible (Vault) |

Each environment has its own Vault with its own password: `group_vars/nonprod/vault.yml` (lab and drills) and `group_vars/production/vault.yml` (`--vault-id production`). The variable names are the same, so the templates do not change.

GitHub keeps only what the deploy needs to connect: the SSH key of `acxess-deploy` and the server address.

## Consequences

- **Positive:** One place to read and change a secret. The Vault files are in Git (encrypted), so a change has a history.
- **Positive:** A release cannot overwrite secrets, and an Ansible run cannot downgrade the app.
- **Positive:** Without the production Vault password, nobody can run the playbook against production by mistake.
- **Negative:** A release that needs a **new** variable in `.env` needs an Ansible run first. `deploy.yml` stops if `.env` does not exist.
- **Negative:** The version that runs in production is on the server and in the GitHub Releases, not in the repository.
- **Later:** with a second person or a second environment, move to SOPS + age (one key per person) or a secrets manager with access logs. The BetterStack token is still in `appsettings.json` and must move to `.env`.
