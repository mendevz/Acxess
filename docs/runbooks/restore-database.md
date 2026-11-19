# Runbook: restore the database

How to bring the database back from the encrypted backup in Cloudflare R2, with [`restore.sh`](../../infrastructure/restore.sh).

Last tested: 2026-11-19, in the lab virtual machine.

## When to use it

- The database is lost or damaged.
- You need the data from an older backup.
- A restore test (we do them on purpose, to know that backups work).

This page covers a server that **already exists**. A new, empty server first needs `ansible-playbook site.yml` ([infrastructure/README.md](../../infrastructure/README.md)).

## Before you start

| You need | Why |
|---|---|
| SSH access as the admin user (with `sudo`) | The script runs as root. |
| The age **private** key | It decrypts the backups. It is **not** on the server. The project owner keeps it. |
| About 5 minutes | Measured: 14 seconds for the restore. The rest is preparation. |

**Warning:** the restore **replaces** the current database. Everything written after the backup is lost. The web app is down for a few seconds.

## Steps

### 1. Copy the key to the server memory

From your machine:

```bash
scp ~/.ssh/<age-key-file> <admin-user>@<server>:/dev/shm/acxess-restore.key
```

`/dev/shm` is memory, not disk. The script refuses a key in any other folder.

### 2. Run the restore

On the server:

```bash
sudo acxess-restore /dev/shm/acxess-restore.key
```

This uses the **newest** backup. To use an older one, list the backups and pass the file name:

```bash
sudo -u acxess-backup rclone lsf <remote>:<bucket>/sql-server
sudo acxess-restore /dev/shm/acxess-restore.key acxess_2026-11-18_03-00-01.bak.age
```

`<remote>:<bucket>/sql-server` is the `RCLONE_REMOTE` value in `/opt/acxess-backup/.env`.

### 3. Read the result

A good restore looks like this:

```text
[13:49:27] Backup: acxess_2026-11-19_13-30-47.bak.age
[13:49:27] Downloading and decrypting into the container
[13:49:29] Identity deleted
The backup set on file 1 is valid.
[13:49:29] Stopping the web app
[13:49:29] Restoring AcxessDB
RESTORE DATABASE successfully processed 1274 pages in 0.026 seconds
[13:49:30] Checking the database
ONLINE SIMPLE
6 422
[13:49:38] Waiting for the app to answer
[13:49:40] Restore completed in 14 s (prepare 2 s, restore and checks 1 s, app 10 s)
```

Check that the backup date is the one you expect, and that the numbers of tenants and members make sense.

## Done when

| Check | Expected |
|---|---|
| Last line of the script | `Restore completed in ... s` |
| Database state | `ONLINE` |
| Tenants and members | Numbers you expect for that date |
| `ls -la /dev/shm/` | The key is **not** there |
| The app in the browser | You can sign in and see members |

On a **new** server, all users must sign in again. The sign-in keys live in Redis, and Redis has no backup. No data is lost.

## If something fails

The script stops at the first error. The table says if the app is still running.

| Error | Meaning | App | What to do |
|---|---|---|---|
| `the age identity must be in /dev/shm` | The key is in another folder. | Up | Copy it to `/dev/shm`. |
| `age: ... unknown identity type` | The file is not an age key (for example, the Ansible Vault password). | Up | Copy the right file. Check it with `age-keygen -y <file>`: the public key must be the same as `backup_age_recipient` in `group_vars/all/vars.yml`. |
| `age: ... no identity matched any of the recipients` | A real age key, but not the one that encrypted the backups. | Up | Same as above. |
| `RESTORE VERIFYONLY` fails | The backup file is damaged. | Up | Restore an older backup (step 2). |
| `only ... MB free` | Not enough disk space. | Up | Free space, then run again. |
| An error after `Stopping the web app` | The restore failed. The database can be unusable. | **Down** | Read the error, fix it and run the script again. |
| `the app does not answer after 60 s` | The database is back, but the app does not start. | Down | `cd /opt/acxess && docker compose logs acxess-web` |

The script deletes the key in every case, even when it fails. To run it again, copy the key again (step 1).

## Measured

| Date | Where | Backup size | Total | Prepare | Restore and checks | App start |
|---|---|---|---|---|---|---|
| 2026-11-19 | Lab VM | 10 MB | 14 s | 2 s | 1 s | 10 s |

The restore from an **empty** server (Ansible + this script) is not measured yet.
