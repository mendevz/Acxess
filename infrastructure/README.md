# infrastructure/

Everything needed to set up and run the Acxess server. For the big picture, read [docs/infrastructure.md](../docs/infrastructure.md) first.

## What is here

| Path | What it is | Used by |
|---|---|---|
| `Caddyfile` | Reverse proxy and HTTPS config | `deploy.yml` copies it to production |
| `backup.sh` | Current production database backup | `deploy.yml` copies it to production |
| `backup-v2.sh` | New database backup: encrypted, checked, with alerts | Ansible (lab). Replaces `backup.sh` after the restore test. |
| `photos-backup.sh` | Copies the photos bucket to a backup bucket | Ansible (lab) |
| `ansible/` | Server setup as code | You, from your machine |
| `lab/cloud-init.yaml` | Creates the lab virtual machine | Multipass |
| `scripts/inventory-server.sh` | Read-only report of a server (users, ports, services) | You, before changing a server |

**Careful:** `deploy.yml` copies `backup.sh` and `Caddyfile` to production on **every release**. A change in these two files goes to production with the next release. New scripts use other names until they are ready.

## Ansible

### Requirements

- Ansible (tested with `ansible-core` 2.20)
- The collections: `ansible-galaxy collection install -r requirements.yml`
- The Vault password in `~/.ansible-vault-pass` (not in Git)
- SSH access to the target server

### Run it

Run the commands **inside** `infrastructure/ansible/`. Ansible reads `ansible.cfg` from the current folder, and that file sets the inventory and the Vault password file. From another folder, Ansible does not find them.

```bash
cd infrastructure/ansible
ansible-playbook site.yml --check --diff   # show what would change, change nothing
ansible-playbook site.yml                  # apply
ansible-playbook site.yml                  # run again: expect changed=0
```

### Files

| File | What it is |
|---|---|
| `site.yml` | The playbook: users, SSH, firewall, Docker, backup tools, backup timers |
| `inventory.ini` | Which servers, and how to connect. Today: the lab VM only. |
| `group_vars/all/vars.yml` | Normal settings (bucket names, paths, public age key) |
| `group_vars/all/vault.yml` | Secrets, encrypted with Ansible Vault |
| `templates/` | Files that Ansible fills in and copies to the server (`.env`, rclone config, systemd units) |

Edit the secrets with:

```bash
EDITOR=nano ansible-vault edit group_vars/all/vault.yml
```

### Lab only

`network_mtu` in `inventory.ini` fixes a network problem of the lab machine. The task runs only when the variable exists, so production is not affected.

## Lab virtual machine

```bash
multipass launch 24.04 --name acxess-lab --cpus 2 --memory 4G --disk 20G \
  --cloud-init infrastructure/lab/cloud-init.yaml --timeout 600
```

Same Ubuntu version as production. The lab VM copies production on purpose (for example, password login is on) so the playbook can be tested before it touches a real server.
