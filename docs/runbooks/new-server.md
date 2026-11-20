# Runbook: bring Acxess back on a new server

How to go from an empty server to a working Acxess with the data of the last backup. Two commands do the work: `ansible-playbook site.yml` and `acxess-restore`.

Last tested: 2026-11-19, in a lab virtual machine (3 drills). Result: **5 min 38 s** from "the server is dead" to "I can sign in".

## When to use it

- The server is lost (hardware, provider, mistake).
- We move to another VPS provider.
- We create a new environment (for example, staging).
- A drill: we practice on purpose, to keep this page true.

To restore the database on a server that still works, use [restore-database.md](restore-database.md).

## Environments

The steps are the same everywhere. These parts change:

| | Lab | Staging (future) | Production |
|---|---|---|---|
| Create the server | `multipass launch` | VPS panel | VPS panel |
| Ansible inventory group | `lab` | `staging` | `production` |
| Name → server | `/etc/hosts` on your machine | DNS record of a test subdomain | DNS record of the real domain |
| TLS certificate | Caddy internal CA (browser warning) | Let's Encrypt **staging** | Let's Encrypt |
| Logs to BetterStack | Blocked (Compose override) | Blocked | **Yes** |
| SMS (Twilio) | Off | Off | **On** |
| Tested | **Yes** | No | No (drill on a real VPS is next) |

## Before you start

| You need | Check |
|---|---|
| This repository, on the commit that runs in production | `git status` is clean |
| Ansible and its collections | `ansible-galaxy collection install -r infrastructure/ansible/requirements.yml` |
| The Ansible Vault password | In `~/.ansible-vault-pass` |
| The age **private** key (decrypts the backups) | `age-keygen -y <key-file>` prints the same public key as `backup_age_recipient` in `group_vars/all/vars.yml` |
| An SSH key for the new server | You can log in with it |

The project owner keeps the Vault password and the age key. They are **never** on a server and never in this repository.

## Keep a timeline

Write down the time of each step. After the incident, the timeline tells you the real recovery time (RTO) and where the time went.

```bash
mark() { echo "$(date '+%F %T') $*" | tee -a ~/incident-$(date +%F).log; }
mark "T0: the server is dead"
```

T0 is the moment the server **stopped working**, not the moment you started to fix it.

## Steps

### 1. Create the server

Ubuntu 24.04, 2 CPUs, 4 GB RAM, 20 GB disk or more.

Lab:

```bash
mark "1 create server"
multipass launch 24.04 --name <vm-name> --cpus 2 --memory 4G --disk 20G \
  --cloud-init infrastructure/lab/cloud-init.yaml --timeout 600
NEW_IP=$(multipass info <vm-name> | awk '/IPv4/ {print $2; exit}')
echo "$NEW_IP"
```

VPS: create it in the provider panel with your SSH key, then `NEW_IP=<server-ip>`.

### 2. First connection

```bash
mark "2 first connection"
ssh-keygen -R "$NEW_IP"
ssh -i <ssh-key> <user>@"$NEW_IP" 'hostname'
```

- `ssh-keygen -R` removes an old fingerprint for this IP, if there is one. A new server has a new fingerprint.
- Answer `yes` to the fingerprint question. If you skip this, Ansible waits for an answer and does not continue.

### 3. Point Ansible to the server

In `infrastructure/ansible/inventory.ini`, under the right group (`[lab]`, `[staging]` or `[production]`), set `ansible_host` to the new IP.

### 4. Command 1: set up the server

```bash
mark "3 ansible start"
cd infrastructure/ansible
ansible-playbook site.yml
cd ../..
mark "3 ansible end"
```

Expected: `failed=0`. Lab time: about 1 min 20 s.

### 5. Command 2: restore the data

```bash
mark "4 restore start"
scp -i <ssh-key> <age-key-file> <user>@"$NEW_IP":/dev/shm/acxess-restore.key
ssh -t -i <ssh-key> <user>@"$NEW_IP" 'sudo acxess-restore /dev/shm/acxess-restore.key'
mark "4 restore end"
```

Expected: `Restore completed in ... s`. Lab time: about 1 min 50 s, most of it to download the Docker images. If something fails, see [restore-database.md](restore-database.md#if-something-fails).

### 6. Send the name to the new server

```bash
mark "5 name"
```

- **Lab:** on your machine, `sudo nano /etc/hosts` and change the IP of the line `acxess-lab.test`. Your computer reads this file **before** it asks DNS, so this line works like a private DNS record.
- **Staging and production:** change the `A` record of the domain in the DNS provider to `NEW_IP`. Caddy gets the Let's Encrypt certificate when the first requests arrive. *Not tested yet.*

```bash
curl -k -sS -o /dev/null -w "%{http_code}\n" https://<domain>/     # 302 = the app answers
```

### 7. Check the app

Open the domain in the browser, sign in and look at the members. Everybody must sign in again: the sign-in keys live in Redis, and Redis has no backup. No data is lost.

```bash
mark "6 END: service is back"
```

## After the service is back

The server works, but it must also be **protected** again.

| Check | Command | Expected |
|---|---|---|
| The server matches the code | `ansible-playbook site.yml` | `changed=0` |
| The new server makes backups | `sudo systemctl start acxess-backup.service`, then `journalctl -u acxess-backup.service -n 3` | `Finished`, and an "OK" in healthchecks.io |
| Network (lab only) | `cat /sys/class/net/eth0/mtu` | `1400` |
| The timeline | `cat ~/incident-<date>.log` | RTO = from T0 to END |

## Production only: extra steps

*Not tested yet. The drill on a real VPS will test them.*

| Step | Why |
|---|---|
| **Stop the old server for good** (power off in the panel, or remove its network access) | If the old server comes back, two servers run the nightly jobs: double backups and **double SMS to the members**. |
| Update the server address in GitHub Secrets (used by `deploy.yml`) | If not, the next release goes to the old server. |
| Lower the DNS TTL before a planned move | The change reaches users faster. |
| First access on a new VPS is often `root` only | The playbook turns off SSH login for `root`. The playbook does not create the admin user yet, so you can lose access. Fix this before the first real VPS. |

## Measured

| Date | Where | From T0 to END | Machine time | Human time | Notes |
|---|---|---|---|---|---|
| 2026-11-19 | Lab VM | 8 min 17 s | ~3 min | ~5 min | One problem: the restore had no login for the private image registry. Fixed. |
| 2026-11-19 | Lab VM | 5 min 36 s* | ~4 min | ~1 min 45 s | No problems. *Measured from "create server", T0 missing. |
| 2026-11-19 | Lab VM | **5 min 38 s** | ~4 min | ~1 min 40 s | No problems. |

## Done when

- You can sign in and see the members.
- The last changes before the backup are there. The changes after it are lost: that is the RPO.
- A second `ansible-playbook site.yml` gives `changed=0`.
- The new server made a backup and healthchecks.io got the "OK".
