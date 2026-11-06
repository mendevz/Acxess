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

- [x] **T0.1** Crear una VM: `multipass launch 24.04 --name acxess-lab --cpus 2 --memory 4G --disk 20G --cloud-init infra/lab/cloud-init.yaml` (misma versión que el VPS: Ubuntu 24.04). Listo cuando: `multipass list` la muestra en `Running`.
- [x] **T0.2** Entrar por SSH a la VM con mi llave (no con `multipass shell`). Listo cuando: `ssh ubuntu@<ip-de-la-vm>` funciona.
- [x] **T0.3** Instalar Ansible en mi máquina. Listo cuando: `ansible --version` responde.
- [ ] **T0.4** Crear `infra/ansible/inventory.ini` con la VM. Listo cuando: `ansible all -i inventory.ini -m ping` devuelve `pong`.
- [ ] **Nota mía:** `notes/ansible-basico.md` con: qué es un inventory, qué es un playbook, qué significa idempotente (con mis palabras).

## Fase 1 - Inventario del servidor real (solo lectura)

- [ ] **T1.1** Correr en el VPS un script de inventario (usuarios, SSH, firewall, Docker, cron, directorios). Listo cuando: tengo la salida guardada, sin secretos.
- [ ] **T1.2** Escribir `docs/runbooks/current-server-state.md` con lo que hay hoy.
- [ ] **Post posible:** "Perdí el rastro de cómo configuré mi servidor".

## Fase 2 - El servidor, como código (en la VM)

- [ ] **T2.1** Playbook que crea el usuario `acxess-deploy` con su llave. Listo cuando: puedo entrar con esa llave.
- [ ] **T2.2** Endurecer SSH (sin contraseña, sin root). **Cuidado:** probar con una segunda sesión abierta para no quedarme fuera.
- [ ] **T2.3** Firewall `ufw`. Anotar en la nota: Docker publica puertos saltándose `ufw`.
- [ ] **T2.4** Instalar Docker y dar permisos a `acxess-deploy`.
- [ ] **T2.5** Crear `/opt/acxess` con los permisos correctos e instalar `rclone`.
- [ ] **T2.6** Correr el playbook otra vez. Listo cuando: el resumen dice `changed=0`.
- [ ] **Post posible:** "Idempotencia: correr el mismo playbook dos veces".

## Fase 3 - Backups completos

- [ ] **T3.1** `backup.sh`: `rclone copy` (no `sync`) y archivos cifrados con `age`.
- [ ] **T3.2** Programar el backup (timer de systemd o cron) desde Ansible.
- [ ] **T3.3** Copiar también el bucket de fotos a otro bucket.
- [ ] **T3.4** Aviso si el backup deja de correr (healthchecks.io).
- [ ] **T3.5** Segunda copia en otro proveedor (regla 3-2-1).
- [ ] **Nota mía:** `notes/backups-3-2-1.md`.

## Fase 4 - Restauración

- [ ] **T4.1** `restore.sh`: restaura el último backup de la base en la VM.
- [ ] **T4.2** Simulacro: VM nueva, playbook, restore, app arriba. Listo cuando: puedo iniciar sesión y ver socios. Anotar **RTO** y **RPO**.
- [ ] **T4.3** `docs/runbooks/nuevo-servidor.md`, con pasos que otra persona pueda seguir.
- [ ] **Post posible:** "Un backup que nunca restauré no es un backup".

## Fase 5 - Ambiente de desarrollo

- [ ] **T5.1** `Makefile` con `make dev`, `make backup`, `make restore`.
- [ ] **T5.2** Datos de prueba con los seeders. **No** restaurar datos reales de socios en desarrollo.

## Fase 6 - Opcional: Terraform

Solo cuando haya más de un recurso de nube que crear (VPS, DNS, R2). Hoy es un servidor; Ansible es suficiente.

## Registro de avance

| Fecha | Tarea | Notas / aprendizaje |
|---|---|---|
| 2026-11-06 | T0.1, T0.2 | VM `acxess-lab` (Ubuntu 24.04) creada con Multipass y cloud-init; acceso por SSH con llave propia del laboratorio. |
| 2026-11-06 | T0.3 | Ansible core 2.20.1 instalado con `apt` en la máquina de control. |
