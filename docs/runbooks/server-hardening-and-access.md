# Runbook: Hardening de SSH y Accesos al Servidor

## Contexto y Problema
Anteriormente, el acceso administrativo y los despliegues de CI/CD se realizaban directamente mediante el usuario `root` y con autenticación por contraseña. Esto exponía el servidor a ataques automatizados de fuerza bruta sobre el puerto 22 y violaba el principio de menor privilegio al darle permisos totales a los jobs automatizados de GitHub Actions. Además, la contraseña de la base de datos estaba expuesta en texto plano dentro del script de respaldo versionado en Git.

## Decisiones Tomadas
1. **Acceso Humano (`maume`):** Acceso administrativo con permisos de `sudo` restringido exclusivamente a autenticación por llaves SSH criptográficas (Ed25519) con passphrase local.
2. **Acceso CI/CD (`acxess-deploy`):** Usuario de servicio no interactivo sin contraseña asignada, limitado exclusivamente a ejecutar comandos Docker y gestionar `/opt/acxess`. Llave SSH dedicada sin passphrase administrada vía GitHub Secrets.
3. **Cierre de Vectores de Ataque:** Desactivación global de inicio de sesión por contraseña (`PasswordAuthentication no`) y bloqueo total de login directo como superusuario (`PermitRootLogin no`).
4. **Aislamiento de Secretos:** `backup.sh` ahora carga dinámicamente credenciales desde el archivo `.env` del host.
---

## Procedimiento de Réplica (Para nuevos servidores o auditoría)

### 1. Configurar Usuario Humano Administrador
```bash
adduser maume
usermod -aG sudo maume
usermod -aG docker maume

# Configurar llaves
su - maume
mkdir -p ~/.ssh && chmod 700 ~/.ssh
touch ~/.ssh/authorized_keys && chmod 600 ~/.ssh/authorized_keys
# (Pegar llave pública de maume en authorized_keys)
exit
```

### 2. Configurar Usuario de Despliegue (CI/CD)

```bash
sudo adduser --disabled-password --gecos "" acxess-deploy
sudo usermod -aG docker acxess-deploy
sudo chown -R acxess-deploy:docker /opt/acxess
sudo chmod -R 775 /opt/acxess
# Configurar llaves para acxess-deploy
sudo su - acxess-deploy
mkdir -p ~/.ssh && chmod 700 ~/.ssh
touch ~/.ssh/authorized_keys && chmod 600 ~/.ssh/authorized_keys
# (Pegar llave pública de acxess-deploy en authorized_keys)
exit
```

### 3. Hardening del demonio SSH (`/etc/ssh/sshd_config`)
Aplicar los siguientes valores:
```text
PermitRootLogin no
PasswordAuthentication no
PubkeyAuthentication yes
```

Validar y reiniciar servicio:
```bash
sshd -t
systemctl restart ssh
```

## Verificación (Smoke Test)
* Conexión como root debe ser rechazada: `ssh root@<IP>` -> Permission denied.
* Conexión de usuario con llave debe ser exitosa: `ssh maume@<IP>`.
* Permisos de Docker sin sudo para deploy: `ssh -i <key> acxess-deploy@<IP> "docker ps"`.