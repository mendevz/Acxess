# Acxess DevOps Lab - Roadmap

## Meta

> Con un servidor vacío y el último backup, Acxess vuelve a funcionar con dos comandos.

Cómo se mide: **RTO** (cuánto tardo en volver a tener la app arriba) y **RPO** (cuántos datos como máximo pierdo). Se anotan al final de la Fase 4.

## Reglas del laboratorio

1. Producción (2 gimnasios reales) no se usa para experimentar. Todo se prueba primero en una VM de Multipass.
2. Cada tarea termina con algo que se puede **verificar** ("Listo cuando").
3. Cada fase termina con una nota escrita **por mí** en `notes/` y, si aplica, un post.
4. No se commitean secretos, IPs reales ni datos de clientes.

## Cómo trabajamos cada tarea

1. Se explica el objetivo y el porqué.
2. Yo escribo los comandos y el código (no copio y pego sin entender).
3. Verifico con el "Listo cuando".
4. Escribo en mis palabras qué aprendí, en `notes/` (ignorada por git, son borradores).
5. Commit pequeño con mensaje claro. Una rama por fase (`devops/fase-N-...`) y PR a `main`.

Las notas pulidas pasan a `docs/` y de ahí salen los posts.

## Conceptos mínimos

| Herramienta | Para qué sirve | Idea clave |
|---|---|---|
| **Multipass** | Crea VMs Ubuntu desechables en mi máquina | Es el "servidor de pruebas" |
| **Ansible** | Describe el estado de un servidor en archivos y lo aplica por SSH | **Idempotencia**: correrlo dos veces da el mismo resultado; la segunda vez no cambia nada |
| **Terraform** | Crea recursos de nube (VPS, DNS, buckets) desde archivos | Aún **no** se usa; ver Fase 6 |
| **age** | Cifra archivos con una llave | Los backups salen cifrados |

Vocabulario de Ansible: **inventory** (lista de servidores), **playbook** (archivo con lo que debe quedar hecho), **task** (un paso), **module** (lo que ejecuta el paso, ej. `apt`, `user`), **handler** (acción que corre solo si algo cambió, ej. reiniciar sshd), **role** (carpeta reutilizable), **vault** (secretos cifrados).

## Fase 0 - Laboratorio listo

- [x] **T0.1** Crear una VM: `multipass launch 24.04 --name acxess-lab --cpus 2 --memory 4G --disk 20G --cloud-init infrastructure/lab/cloud-init.yaml` (misma versión que el VPS: Ubuntu 24.04). Listo cuando: `multipass list` la muestra en `Running`.
- [x] **T0.2** Entrar por SSH a la VM con mi llave (no con `multipass shell`). Listo cuando: `ssh ubuntu@<ip-de-la-vm>` funciona.
- [x] **T0.3** Instalar Ansible en mi máquina. Listo cuando: `ansible --version` responde.
- [x] **T0.4** Crear `infrastructure/ansible/inventory.ini` con la VM. Listo cuando: `ansible all -i infrastructure/ansible/inventory.ini -m ping` devuelve `pong`.
- [ ] **Nota mía:** `notes/ansible-basico.md` con: qué es un inventory, qué es un playbook, qué significa idempotente (con mis palabras).

## Fase 1 - Inventario del servidor real (solo lectura)

- [x] **T1.1** Correr en el VPS un script de inventario (usuarios, SSH, firewall, Docker, cron, directorios). Listo cuando: tengo la salida guardada, sin secretos.
- [x] **T1.2** Escribir `docs/runbooks/current-server-state.md` con lo que hay hoy.
- [ ] **Post posible:** "Perdí el rastro de cómo configuré mi servidor".

## Fase 2 - El servidor, como código (en la VM)

- [x] **T2.1** Playbook que crea el usuario `acxess-deploy` con su llave. Listo cuando: puedo entrar con esa llave.
- [x] **T2.2** Endurecer SSH (sin contraseña, sin root). **Cuidado:** probar con una segunda sesión abierta para no quedarme fuera.
- [x] **T2.3** Firewall `ufw`. Anotar en la nota: Docker publica puertos saltándose `ufw`.
- [x] **T2.4** Instalar Docker y dar permisos a `acxess-deploy`.
- [x] **T2.5** Crear `/opt/acxess` con los permisos correctos e instalar `rclone`.
- [x] **T2.6** Correr el playbook otra vez. Listo cuando: el resumen dice `changed=0`.
- [ ] **Post posible:** "Idempotencia: correr el mismo playbook dos veces".

## Fase 3 - Backups completos

- [ ] **T3.0** Red de la VM: el MTU efectivo es 1460 pero `eth0` anuncia 1500, y la negociación TLS tarda ~4 s (`docker pull` y `apt` se cuelgan).
  Solución prevista: variable opcional `network_mtu` (netplan), definida **solo en el inventario del laboratorio**. Producción no la usa. Explicación en `notes/11-mtu-red-de-la-vm.md`.
  Listo cuando: en una VM nueva, tras el playbook, `curl -w "%{time_appconnect}"` a `downloads.rclone.org` da menos de 0.5 s y `docker pull alpine` funciona sin ayuda.
- [x] **T3.1** `backup.sh`: `rclone copy` (no `sync`) y archivos cifrados con `age`.
  - [x] **T3.1a** Par de llaves `age` creado (la privada solo en mi máquina y en el gestor de contraseñas).
  - [x] **T3.1b** Reescribir `backup.sh` por pasos (configuración, dump con `CHECKSUM`, cifrado en streaming, `rclone copy`, retención), probado en la VM con un contenedor de pega.
- [x] **T3.2a** Secretos con Ansible Vault (credenciales de R2, URL de healthchecks). Se necesita antes de T3.2: un servidor nuevo no tiene ningún secreto. La llave privada de `age` **no** va en el servidor; se instala solo al restaurar (Fase 4).
- [x] **T3.2** Programar el backup (timer de systemd o cron) desde Ansible. Incluye instalar el paquete `age` (el script falla sin él: se descubrió probando en la VM) e instalar `rclone` desde rclone.org con versión fijada (**v1.76.0**, ya probada a mano en la VM) y comprobando su suma SHA256: la 1.60.1 de `apt` devuelve `501 NotImplemented` con R2.
  **Decisión:** se instala el paquete `unzip` con `apt` y se usa el módulo `unarchive` de Ansible (que lo necesita para `.zip`). No se usa `python3 -m zipfile`: solo sirvió para salir del paso en la VM cuando la red fallaba, y obligaría a un `command` que no es idempotente.
- [ ] **T3.3** Copiar también el bucket de fotos a otro bucket.
- [ ] **T3.4** Aviso si el backup deja de correr (healthchecks.io).
- [ ] **T3.5** Segunda copia en otro proveedor (regla 3-2-1).
- [ ] **T3.6** Pasar producción al backup nuevo (`backup-v2.sh` pasa a ser `backup.sh`). **No** antes de la Fase 4 (restore probado). `deploy.yml` sube `infrastructure/backup.sh` a producción en cada release, por eso el script nuevo vive en `backup-v2.sh` hasta este momento.
  Requisitos en el VPS: paquete `age`, `rclone` v1.76.0, `AGE_RECIPIENT`, `RCLONE_REMOTE` y `BACKUP_DIR` en el `.env` (el workflow lo regenera: añadirlas ahí), remoto de rclone configurado para el usuario que ejecute el backup (hoy `r2-acxess`, solo de root), y el aviso de T3.4 activo **antes** del cambio.
  Listo cuando: una noche completa corre sola, llega el `.age` a R2 y healthchecks lo registra.
- [ ] **Nota mía:** `notes/backups-3-2-1.md`.

## Fase 4 - Restauración

- [ ] **T4.1** `restore.sh`: restaura el último backup de la base en la VM.
- [ ] **T4.2** Simulacro: VM nueva, playbook, restore, app arriba. Listo cuando: puedo iniciar sesión y ver socios. Anotar **RTO** y **RPO**.
- [ ] **T4.3** `docs/runbooks/nuevo-servidor.md`, con pasos que otra persona pueda seguir.
- [ ] **T4.4** Decisión (ADR): dónde viven las llaves de DataProtection. Opciones: Redis persistente y cifrado (`ProtectKeysWith...`, `appendonly yes`) o SQL Server (`PersistKeysToDbContext`, entran en el backup).
  Objetivo: que **el restore sea un solo comando** (`make restore` o `restore.sh`) sin pasos manuales para llaves ni sesiones.
  Para los simulacros: usar un subdominio de prueba o el CA *staging* de Let's Encrypt (límite de certificados duplicados).
- [ ] **Post posible:** "Un backup que nunca restauré no es un backup".

## Fase 5 - Ambiente de desarrollo

- [ ] **T5.1** `Makefile` con `make dev`, `make backup`, `make restore`.
- [ ] **T5.2** Datos de prueba con los seeders. **No** restaurar datos reales de socios en desarrollo.

## Fase 6 - Opcional: Terraform

Solo cuando haya más de un recurso de nube que crear (VPS, DNS, R2). Hoy es un servidor; Ansible es suficiente.

## Backlog 

- [x] **B1** No ejecutar las pruebas de .NET cuando el PR no toca código de la aplicación.
  Hoy `ci-pr.yml` corre build y tests completos (con Testcontainers) aunque el PR solo cambie `docs/` o `infrastructure/`.
  Solución prevista: `paths-ignore` en el trigger `pull_request` con `docs/**`, `infrastructure/**` y `**.md`.
  Listo cuando: un PR que solo toca `docs/` no dispara el workflow, y uno que toca `src/` sí.
  Ojo: si algún día se exige el check como obligatorio en una regla de protección de rama, un workflow omitido por `paths-ignore`
  se queda como "pendiente" y bloquea el merge. Habría que resolverlo en ese momento.

- [ ] **B2** Backup previo a migraciones, bien diseñado (Fase 3): un solo mecanismo con identidad propia; el deploy solo verifica que existe un backup reciente.
- [x] **B3** Reinicio del servidor en ventana de mantenimiento (backup antes, `restart:` en compose verificado).
- [ ] **B4** Limpieza automática de imágenes Docker antiguas y rotación de `cron.log`.
  Hecho a mano el 2026-11-07 (11.4 GB liberados). Falta que el deploy use `docker image prune -a -f --filter "until=720h"` y configurar `logrotate` para `cron.log`.
- [ ] **B5** ADR: `acxess-deploy` en el grupo `docker` (riesgo aceptado y mitigaciones).

- [ ] **B7** `deploy.yml` crea el `.env` con `cat > .env`, y un archivo nuevo toma el permiso por defecto (`664`). Añadir `umask 077` antes de crearlo o `chmod 600 .env` después. Hoy lo protege el permiso del directorio `/opt/acxess` (`750`, T2.5), pero no debe depender de eso.
- [ ] **B6** Laboratorio futuro de escalado: 2 instancias de la app + balanceador + base de datos en una VM aparte (red privada). Requiere app stateless (DataProtection compartido, ver T4.4) y TLS terminado en el balanceador.

## Registro de avance

| Fecha | Tarea | Notas / aprendizaje |
|---|---|---|
| 2026-11-06 | T0.1, T0.2 | VM `acxess-lab` (Ubuntu 24.04) creada con Multipass y cloud-init; acceso por SSH con llave propia del laboratorio. |
| 2026-11-06 | T0.3 | Ansible core 2.20.1 instalado con `apt` en la máquina de control. |
| 2026-11-06 | T0.4 | Inventario creado; `ansible -m ping` responde `pong` desde la VM. Se unifican las rutas en `infrastructure/`. |
| 2026-11-07 | T1.1, T1.2 | Inventario del VPS corrido dos veces. Se corrigen SSH por contraseña, permisos de `.env` y volúmenes huérfanos. Runbook `current-server-state.md` creado. |
| 2026-11-07 | B3 | Reinicio planeado del VPS: kernel actualizado, 4 contenedores arriba solos en ~3 min. |
| 2026-11-08 | T2.1 | Playbook `site.yml` crea `acxess-deploy` y su llave en la VM. Primera pasada `changed=2`, segunda `changed=0`. `--check` falla en un servidor limpio cuando una tarea depende de otra. |
| 2026-11-08 | T2.2 | VM recreada desde cero con `cloud-init` que simula el VPS (contraseña habilitada) y llaves nuevas (humana con passphrase, deploy sin passphrase). El playbook endurece SSH con `validate` y handler; la contraseña deja de funcionar y la segunda pasada da `changed=0`. |
| 2026-11-09 | T2.3 | Firewall `ufw` por playbook: SSH, HTTP y HTTPS permitidos, resto de entrada denegado, activación al final para no perder la sesión. Segunda pasada `changed=0`. Pendiente: política `routed` (diferencia con producción) al instalar Docker. |
| 2026-11-09 | T2.4 | Docker 29.9.3 y Compose v5.7.0 instalados desde el repositorio oficial por playbook (mismas versiones que producción). `acxess-deploy` usa `docker ps` sin `sudo`. Se sustituye `ansible_distribution_release` por `ansible_facts['distribution_release']`. |
| 2026-11-10 | T2.5 | `/opt/acxess` creado por playbook con `0750` y dueño `acxess-deploy` (en producción era `775` con grupo `docker`); `rclone` 1.60.1 instalado. Otro usuario no puede listar la carpeta. Se anota B7 (permiso del `.env` en el workflow). |
| 2026-11-10 | T2.6 | Prueba de fuego en una VM nueva y vacía: el playbook completo converge solo (`changed=12`) y la segunda pasada da `changed=0`. Se añaden `requirements.yml` y `ansible.cfg`. Fase 2 completa. |
| 2026-11-11 | T3.1a | Llaves `age` creadas y probadas (cifrar con la pública, descifrar con la privada). Con `rclone` y R2: `copy` no borra en destino, `sync --dry-run` anuncia el borrado. `rclone` 1.60.1 de `apt` da `501` al subir; el binario oficial no. |
| 2026-11-11 | T3.0 (diagnóstico) | `apt`, `rclone` y `docker pull` lentos o colgados. El IPv6 se descartó (falla en 2 ms). Camino de 1460 bytes de MTU máximo con `eth0` en 1500: con `mtu 1400` temporal, el TLS pasó de 3.9 s a 0.10 s. Falta hacerlo persistente por Ansible. |
| 2026-11-12 | T3.1b | `backup-v2.sh` (nombre aparte: `deploy.yml` sube `backup.sh` a producción en cada release). Probado en la VM con un contenedor de pega y un `sqlcmd` falso: cifrado en streaming (round trip exacto de 5 242 880 bytes), `rclone copy` de la carpeta, `check` antes de rotar, `.part` y `trap` para no subir ni dejar archivos a medias. Se corrigió un fallo de `find` cuando el directorio de trabajo no era accesible (`cd "$SCRIPT_DIR"`). |
| 2026-11-12 | T3.2a, T3.2 | Ansible Vault para credenciales de R2 y `DB_PASSWORD`. Playbook instala `age`, `unzip`, `rclone` v1.76.0 (con SHA256), usuario `acxess-backup` (`nologin`, grupo `docker`), script de root solo lectura, `.env` `0640`, `rclone.conf` `0600`, `.service` y `.timer` (03:00, `Persistent`). Segunda pasada `changed=0`. Fallo provocado (contenedor detenido): `failed`, error en `journalctl`, nadie avisado: falta T3.4. |
| 2026-11-07 | B4 (parcial) | Limpieza manual de imágenes: de 47 a 4, 11.4 GB liberados. Falta automatizarla. |
