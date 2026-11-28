# Runbook: bring Acxess back on a new server

How to go from an empty server to a working Acxess with the data of the last backup. Two commands do the work: `ansible-playbook site.yml` and `acxess-restore`.

Last used: 2026-11-27, to rebuild **production** from code: **16 min 16 s**, no data lost. Drill on a real VPS (2026-11-24): 10 min 58 s. Lab VM (2026-11-19): 5 min 38 s.

## When to use it

- The server is lost (hardware, provider, mistake).
- We move to another VPS provider.
- We create a new environment (for example, staging).
- A drill: we practice on purpose, to keep this page true.

To restore the database on a server that still works, use [restore-database.md](restore-database.md).

## Environments

The steps are the same everywhere. These parts change:

| | Lab | Drill on a VPS (also future staging) | Production |
|---|---|---|---|
| Create the server | `multipass launch` | Hetzner panel | Hetzner panel |
| Ansible inventory | `inventory.ini` (the default) | `-i inventory-drill.ini` | `-i inventory-production.ini --vault-id production@<password-file>` |
| First SSH user | `ubuntu` | `root`, then the admin user | `root`, then the admin user |
| Name → server | `/etc/hosts` on your machine | Cloudflare record `drill`, proxied | Cloudflare record, proxied |
| TLS certificate | Caddy internal CA (browser warning) | Let's Encrypt (Cloudflare mode Full (strict)) | Let's Encrypt (same) |
| Extra password before the app | No | Yes (`basic_auth` in Caddy) | No |
| Logs to BetterStack | Blocked (Compose override) | Blocked | **Yes** |
| SMS (Twilio) | Off | Off | **On** |
| Tested | Yes | Yes (2026-11-24 and 2026-11-27) | Yes (2026-11-27, rebuild of the same server) |

## Before you start

| You need | Check |
|---|---|
| This repository, on the commit that runs in production | `git status` is clean |
| Ansible and its collections | `ansible-galaxy collection install -r infrastructure/ansible/requirements.yml` |
| The Ansible Vault password | In `~/.ansible-vault-pass` |
| An age **private** key (decrypts the backups) | `age-keygen -y <key-file>` prints one of the keys in `backup_age_recipients` in `group_vars/all/vars.yml` (the main key, or the emergency key on paper) |
| An SSH key for the new server | Uploaded to the provider, or in `cloud-init` for the lab |
| The `sudo` password of the admin user | You need it for the restore and for later Ansible runs |

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

**Ubuntu 24.04**, x86, 2 CPUs or more, 4 GB RAM, 20 GB disk or more. Production: Hetzner CPX21 in Ashburn.

Check the image. The panel can offer a newer Ubuntu first. The playbook is tested on 24.04 only: on 26.04, `sudo` is a different program and Ansible cannot type the `sudo` password.

Lab:

```bash
mark "1 create server"
multipass launch 24.04 --name <vm-name> --cpus 2 --memory 4G --disk 20G \
  --cloud-init infrastructure/lab/cloud-init.yaml --timeout 600
export NEW_IP=$(multipass info <vm-name> | awk '/IPv4/ {print $2; exit}')
```

VPS: create it in the panel with your SSH key, then:

```bash
export NEW_IP=<server-ip>
mark "1 server created"
```

`export` matters: the VPS inventory reads the IP from `NEW_IP`.

### 2. Send the name to the new server, now

Do this right after you have the IP. It works while Ansible runs.

- **VPS (Cloudflare):** create or change the `A` record (`drill` for a drill, the real domain for production) to `NEW_IP`, **Proxied**, TTL Auto. Users connect to Cloudflare IPs, which do not change, so the new server gets the traffic in seconds.
- **Lab:** on your machine, `sudo nano /etc/hosts` and change the IP of the line `acxess-lab.test`. Your computer reads this file **before** it asks DNS.

```bash
mark "2 name"
```

### 3. First connection

```bash
ssh-keygen -R "$NEW_IP"
ssh -i <ssh-key> <first-user>@"$NEW_IP" 'hostname'
mark "3 first connection"
```

- `ssh-keygen -R` removes an old fingerprint for this IP, if there is one.
- Answer `yes` to the fingerprint question. If you skip this, Ansible waits for an answer and does not continue.
- `<first-user>` is `ubuntu` in the lab and `root` on a new VPS.
- If `root` asks for a password: the server was created without an SSH key, so the provider has no key to install. On Hetzner: *Rescue → Reset root password*, then `ssh-copy-id -i <ssh-key>.pub root@"$NEW_IP"`. This password stops working after the playbook (step 4). It happened with production on 2026-11-27.

### 4. Command 1: set up the server

```bash
cd infrastructure/ansible
ansible-playbook site.yml                          # lab
ansible-playbook -i inventory-drill.ini site.yml   # VPS drill (first run as root)
ansible-playbook -i inventory-production.ini --vault-id production@<password-file> \
  -e app_version=<last-release> site.yml           # production (first run as root)
cd ../..
mark "4 ansible end"
```

Expected: `failed=0`. Measured: 1 min 20 s in the lab, **about 5 min 45 s** on the VPS.

On a VPS, this run creates the admin user and then turns off SSH login for `root`. From now on, connect as the admin user.

Production: `-e app_version` must be the **last release** (see GitHub Releases). The backup has the database of that version, and the app must match it. On a server that already has a version, Ansible keeps it.

### 5. Command 2: restore the data

```bash
scp -i <ssh-key> <age-key-file> <admin-user>@"$NEW_IP":/dev/shm/acxess-restore.key
ssh -t -i <ssh-key> <admin-user>@"$NEW_IP" 'sudo acxess-restore /dev/shm/acxess-restore.key'
mark "5 restore end"
```

`sudo` asks for the admin password. Expected: `Restore completed in ... s`. Measured: 1 min 50 s in the lab, **34 to 40 s** on the VPS (a data center downloads the images much faster). If something fails, see [restore-database.md](restore-database.md#if-something-fails).

### 6. Check HTTPS

```bash
dig +short <domain> @1.1.1.1
curl -sS -o /dev/null -w "%{http_code}\n" https://<domain>/
curl -sv -o /dev/null --resolve <domain>:443:"$NEW_IP" https://<domain>/ 2>&1 | grep -iE "issuer|HTTP/"
mark "6 https ok"
```

| Check | Expected |
|---|---|
| `dig` | Cloudflare IPs, not the server IP (the record is proxied) |
| `curl` | `302` (the app sends you to the sign-in page). On a drill with `basic_auth`: `401`. |
| `curl --resolve` | Talks to the server directly, without DNS. Issuer: Let's Encrypt. |

While the restore runs, Cloudflare can show `521` (the server does not answer yet). `525` or `526` for a few seconds means Caddy is still getting its certificate. Wait and try again.

Lab: `curl -k -sS -o /dev/null -w "%{http_code}\n" https://acxess-lab.test/`.

### 7. Check the app

Open the domain in the browser, sign in and look at the members. Everybody must sign in again: the sign-in keys live in Redis, and Redis has no backup. No data is lost ([ADR 0005](../adrs/0005-keep-data-protection-keys-in-redis.md)).

```bash
mark "7 END: service is back"
```

## After the service is back

The server works, but it must also be **protected** again.

| Check | Command | Expected |
|---|---|---|
| `root` cannot log in (VPS) | `ssh root@"$NEW_IP"` | `Permission denied` |
| The server matches the code | Lab: `ansible-playbook site.yml`. VPS: `ansible-playbook -i <inventory> site.yml -e ansible_user=<admin-user> -K` | `changed=0` |
| The new server makes backups | `sudo systemctl start acxess-backup.service`, then `sudo journalctl -u acxess-backup.service -n 3` | `Finished`, and an "OK" in healthchecks.io |
| Network | `cat /sys/class/net/eth0/mtu` | `1400` in the lab, `1500` on a VPS |
| The timeline | `cat ~/incident-<date>.log` | RTO = from T0 to END |

## Production only: extra steps

| Step | Why | Tested |
|---|---|---|
| **Stop the old server for good** (power off in the panel, or remove its network access) | If the old server comes back, two servers run the nightly jobs: double backups and **double SMS to the members**. | No |
| Update the server address in GitHub Secrets (used by `deploy.yml`) | If not, the next release goes to the old server. Not needed after a rebuild of the same server (same IP). | No |
| Change the origin IP of the proxied records in Cloudflare | Users keep the Cloudflare IPs, so the change works in seconds. No TTL wait. | Yes, with a new record (drill) |
| First access as `root` | The playbook creates the admin user **before** it turns off `root` login. | Yes (drill and production) |
| Delete the drill server, its IP and its DNS record after a drill | A DNS record that points to an IP the provider gives to someone else is a risk. | Yes |

## Measured

| Date | Where | From T0 to END | Biggest parts | Notes |
|---|---|---|---|---|
| 2026-11-19 | Lab VM | 8 min 17 s | Human work ~5 min | The restore had no login for the private image registry. Fixed. |
| 2026-11-19 | Lab VM | 5 min 36 s* | Machine ~4 min | No problems. *T0 missing. |
| 2026-11-19 | Lab VM | 5 min 38 s | Machine ~4 min | No problems. |
| 2026-11-24 | Hetzner VPS + Cloudflare | **15 min 30 s** | Create the server in the panel 4 min, Ansible 5 min 51 s, restore 34 s | HTTPS and DNS worked at once. Wrong image (Ubuntu 26.04): the second Ansible run as admin failed. |
| 2026-11-24 | Hetzner VPS + Cloudflare, Ubuntu 24.04 | **10 min 58 s** | Create the server 1 min, Ansible 5 min 43 s, restore 40 s | No problems. Second Ansible run as admin: `changed=0`. |
| 2026-11-27 | **Production**, rebuild of the same Hetzner server | **16 min 16 s** | Root access 7 min 29 s, Ansible 2 min 56 s, upload + restore 2 min 13 s | Planned move: app stopped before the last backup, snapshot before the rebuild. No data lost. `root` had no SSH key after the rebuild. |

## Done when

- You can sign in and see the members.
- The last changes before the backup are there. The changes after it are lost: that is the RPO.
- A second Ansible run gives `changed=0`.
- The new server made a backup and healthchecks.io got the "OK".
