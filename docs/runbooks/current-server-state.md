# Estado actual del servidor de producción

Inventario de solo lectura generado con `infrastructure/scripts/inventory-server.sh` el 2026-11-07.
Este documento no incluye IPs, llaves ni contenido de secretos.

## Resumen

| Área | Estado |
|---|---|
| SO | Ubuntu 24.04.5 LTS, kernel 6.8.0-124, zona horaria `America/Mexico_City` |
| Recursos | 3 vCPU, 3.7 GB RAM (2.2 GB disponibles), **sin swap**, disco 75 GB al 40 % |
| Uptime | Reiniciado el 2026-11-07 (antes: 136 días). Kernel `6.8.0-149` |
| Usuarios | `maume` (sudo + docker), `acxess-deploy` (docker). Root por SSH deshabilitado |
| SSH | Puerto 22, solo llave (`passwordauthentication no` desde 2026-11-07, vía `/etc/ssh/sshd_config.d/00-hardening.conf`) |
| Firewall | `ufw` activo, permite 22/80/443. `fail2ban` inactivo |
| Actualizaciones | `unattended-upgrades` activo |
| Docker | 29.9.3, Compose v5.7.0 |

## Contenedores

| Contenedor | Imagen | Puertos publicados |
|---|---|---|
| `caddy_proxy` | `caddy:2-alpine` | 80, 443 |
| `acxess_web` | `ghcr.io/.../acxess-web:v1.12.3` | solo red interna |
| `acxess_migrator` | misma imagen | ninguno (corre y termina) |
| `redis_acxess` | `redis:7-alpine` | **6379 en 0.0.0.0** (ver hallazgos) |
| `sql_acxess_container` | `mssql/server:2022-latest` | 1433 solo en 127.0.0.1 |

Volúmenes: `acxess_sql_data`, `acxess_redis_data`, `acxess_caddy_data`, `acxess_caddy_config`.

## Directorio `/opt/acxess`

Dueño `acxess-deploy`, grupo `docker`, permisos `775`. Contiene `docker-compose.yml`, `Caddyfile`,
`backup.sh`, `.env` (`600`), `backups/`, `data/uploads/` y `docker-compose.yml.save` (copia vieja).

## Backups actuales

- Cron de **root**, todos los días a las 03:00: `/opt/acxess/backup.sh >> /opt/acxess/backups/cron.log`.
- Genera un `.bak` de ~9.6 MB en `/opt/acxess/backups/` (dueño root, `640`) y lo sube a R2 (remoto `r2-acxess`, configurado solo para root).
- `acxess-deploy` y `maume` no tienen configuración de `rclone`.
- Nunca se ha probado una restauración.

## Reinicio del 2026-11-07

- Motivo: kernel actualizado por `apt upgrade` (`6.8.0-124` a `6.8.0-149`).
- Ventana: sábado, hora sin uso de la app. Backup de las 03:00 verificado antes.
- Resultado: los 4 contenedores volvieron solos (`restart` configurado) y `redis` y `sql` quedaron `healthy` a los 2 minutos.
- Tiempo hasta servicio recuperado: unos 3 minutos. Es el primer dato de referencia para el **RTO** de un reinicio.
- Procedimiento: backup, comprobar `restart:` y `docker` habilitado, guardar estado antes, `sudo reboot`, comparar kernel, uptime y contenedores, probar la app.

## Limpieza de imágenes Docker del 2026-11-07

- Antes: 47 imágenes (16.81 GB), 11.39 GB recuperables. Después: 4 imágenes (3.21 GB), 0 B recuperables.
- Comando: `docker image prune -a --filter "until=720h"` (borra imágenes sin contenedor y con más de 30 días).
- Verificado: los 4 contenedores y los 4 volúmenes siguen activos.
- No se usó `docker system prune --volumes` (podría borrar datos).
- Pendiente (B4): el deploy solo ejecuta `docker image prune -f`, que borra únicamente imágenes sin etiqueta. Las versiones viejas se acumularán otra vez. Cambiarlo por `docker image prune -a -f --filter "until=720h"` y rotar `cron.log`. Se hace en la Fase 2/3 (Ansible o workflow).

## Hallazgos y estado

| # | Hallazgo | Riesgo | Estado |
|---|---|---|---|
| H1 | Redis publicado en `0.0.0.0:6379` (Docker salta `ufw`) | Alto | Corregido en `docker-compose.prod.yml`; **pendiente del próximo release** |
| H2 | Acceso SSH por contraseña | Alto | **Resuelto** 2026-11-07 |
| H3 | `.env` con permisos `775` | Alto | **Resuelto** (`600`) |
| H4 | Volúmenes huérfanos | Bajo | **Resuelto** |
| H5 | Reinicio pendiente (kernel actualizado) | Medio | **Resuelto** 2026-11-07 (ver "Reinicio" abajo) |
| H6 | Sin swap | Medio | Pendiente: Fase 2 (Ansible) |
| H7 | `acxess-deploy` en grupo `docker` (equivale a root) | Medio | Aceptado; escribir ADR |
| H8 | `backup.sh` lo ejecuta root pero lo puede modificar el grupo `docker` | Medio | Pendiente: Fase 3 |
| H9 | 47 imágenes Docker, 11 GB recuperables; `cron.log` sin rotación | Bajo | Imágenes **limpiadas** 2026-11-07; falta automatizar y rotar `cron.log` (B4) |
| H10 | `fail2ban` inactivo | Bajo | Opcional (sin contraseñas ya no hay fuerza bruta útil) |
| H11 | Backup sin restauración probada ni copia local rotada | Alto | Fases 3 y 4 |
