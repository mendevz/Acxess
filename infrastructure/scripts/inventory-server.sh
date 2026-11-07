#!/usr/bin/env bash
# Inventario de SOLO LECTURA de un servidor Ubuntu.
# No modifica nada. No imprime secretos: no lee .env, no usa 'docker inspect'
# ni 'rclone config show'.
#
# Uso:  bash inventory-server.sh > inventory.txt
# La salida puede incluir IPs, usuarios y reglas de firewall
# Sin 'set -e' a propósito: si un comando falla o no existe, el inventario sigue.
set -u

section() { printf '\n\n===== %s =====\n' "$1"; }

# Muestra el comando y lo ejecuta; si falla, lo dice y continúa.
run() {
  printf '\n$ %s\n' "$*"
  "$@" 2>&1 || printf '(el comando terminó con error o no está disponible)\n'
}

printf 'Inventario generado: %s en %s\n' "$(date -u +%FT%TZ)" "$(hostname)"

section "Sistema"
run lsb_release -a
run uname -r
run timedatectl
run uptime

section "Recursos"
run nproc
run free -h
run swapon --show
run df -h /

section "Usuarios (UID >= 1000) y grupos relevantes"
run awk -F: '$3>=1000 && $3<65534 {print $1" uid="$3" home="$6" shell="$7}' /etc/passwd
run getent group sudo docker
run id acxess-deploy

section "SSH (configuración efectiva) y llaves"
run bash -c "sudo sshd -T | grep -E '^(port|permitrootlogin|passwordauthentication|pubkeyauthentication|allowusers|allowgroups|maxauthtries) '"
# Solo nombres y permisos de los archivos, nunca el contenido de las llaves.
# El glob debe expandirse DENTRO de sudo; fuera, tu usuario no puede leer /home/otro-usuario.
run sudo bash -c 'ls -la /home/*/.ssh/'
# Archivos que pueden sobrescribir sshd_config (ej. el de cloud-init).
run sudo ls -la /etc/ssh/sshd_config.d/

section "Firewall y puertos en escucha"
run sudo ufw status verbose
run sudo ss -tlnp

section "Docker"
run docker --version
run docker compose version
run sudo docker ps -a --format 'table {{.Names}}\t{{.Image}}\t{{.Status}}\t{{.Ports}}'
run sudo docker volume ls
run sudo docker network ls
run sudo docker system df

section "Directorio de la aplicación (solo nombres y tamaños)"
run ls -la /opt/acxess
run bash -c "ls -lah /opt/acxess/backups | tail -n 5"
run ls -la /opt/acxess/data

section "Tareas programadas"
run sudo ls -la /var/spool/cron/crontabs
# Contenido del cron de root (el archivo es ilegible sin sudo). Revisar antes de compartir.
run sudo crontab -l -u root
run systemctl list-timers --all --no-pager

section "rclone (solo nombres de remotos, nunca credenciales)"
run which rclone
run rclone listremotes
run sudo -u acxess-deploy rclone listremotes
run sudo rclone listremotes

section "Servicios de seguridad y actualizaciones"
run systemctl is-active unattended-upgrades fail2ban ufw docker
run bash -c "ls /var/run/reboot-required 2>/dev/null || echo 'no hay reinicio pendiente'"

section "Paquetes instalados manualmente"
run apt-mark showmanual
