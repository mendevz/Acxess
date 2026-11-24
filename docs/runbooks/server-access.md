# Runbook: server access and SSH hardening

How to set up the server accounts and SSH by hand. The Ansible playbook ([`site.yml`](../../infrastructure/ansible/site.yml)) does the same steps automatically. Use this page to understand them or to check a server.

Last checked: 2026-11-07 (production), 2026-11-10 (lab, with Ansible).

## Why

Before, people and the CI pipeline used `root` with a password. Bots try passwords on port 22 all day. Also, the pipeline had full control of the server, and it only needs Docker.

## The accounts

| Account | For | Rules |
|---|---|---|
| `<admin-user>` | A person | SSH key with passphrase, `sudo` |
| `acxess-deploy` | GitHub Actions | Own SSH key without passphrase (stored in GitHub Secrets), no password, no `sudo` |
| `root` | — | No SSH login |

## Steps

### 1. Admin user

The playbook creates this user now (`admin_user`, SSH key, `sudo` with a password from the Vault). These are the same steps by hand.

```bash
adduser <admin-user>
usermod -aG sudo <admin-user>

su - <admin-user>
mkdir -p ~/.ssh && chmod 700 ~/.ssh
touch ~/.ssh/authorized_keys && chmod 600 ~/.ssh/authorized_keys
# Paste the admin public key into ~/.ssh/authorized_keys
exit
```

**Before you continue:** open a **new** terminal and log in as `<admin-user>` with the key. Keep the old session open. If the new login fails, you can still fix it.

### 2. Deploy user

```bash
sudo adduser --disabled-password --gecos "" acxess-deploy
sudo usermod -aG docker acxess-deploy
sudo mkdir -p /opt/acxess
sudo chown acxess-deploy:acxess-deploy /opt/acxess
sudo chmod 750 /opt/acxess

sudo su - acxess-deploy
mkdir -p ~/.ssh && chmod 700 ~/.ssh
touch ~/.ssh/authorized_keys && chmod 600 ~/.ssh/authorized_keys
# Paste the deploy public key into ~/.ssh/authorized_keys
exit
```

The `docker` group is almost the same as `root`. It is a known and accepted risk: the pipeline needs Docker to deploy.

### 3. Harden SSH

Create `/etc/ssh/sshd_config.d/00-hardening.conf`:

```text
PasswordAuthentication no
KbdInteractiveAuthentication no
PermitRootLogin no
```

The `00-` prefix matters. SSH uses the **first** value it reads, and files in `sshd_config.d/` are read in name order. Cloud images often have a `50-cloud-init.conf` with `PasswordAuthentication yes`.

Check the file and reload:

```bash
sudo sshd -t
sudo systemctl reload ssh
```

### 4. Firewall

```bash
sudo ufw default deny incoming
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
```

**Important:** Docker writes its own firewall rules, and a port published with `ports:` in Docker Compose is open **even if `ufw` blocks it**. So a service that must stay private needs no `ports:` at all, or `127.0.0.1:<port>:<port>` (like SQL Server in this project).

## Done when

| Test | Expected result |
|---|---|
| `ssh root@<server>` | `Permission denied` |
| `ssh -o PubkeyAuthentication=no <admin-user>@<server>` | `Permission denied` (no password login) |
| `ssh <admin-user>@<server>` with the key | Login works |
| `sudo sshd -T \| grep -E 'permitrootlogin\|passwordauthentication'` | Both `no` |
