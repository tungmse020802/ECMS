#!/usr/bin/env bash
set -euo pipefail

CONTAINER_NAME="${ECMS_SQL_CONTAINER:-ecms-sqlserver}"
DB_NAME="${ECMS_DB_NAME:-ECMS}"
DB_USER="${ECMS_DB_USER:-sa}"
DB_PASSWORD="${ECMS_DB_PASSWORD:-EcmsDocker@2026}"
HOST_BACKUP_DIR="${ECMS_BACKUP_DIR:-./backups}"
CONTAINER_BACKUP_DIR="${ECMS_CONTAINER_BACKUP_DIR:-/var/opt/mssql/backup}"
TIMESTAMP="$(date -u +%Y%m%dT%H%M%SZ)"
BACKUP_FILE="${DB_NAME}_${TIMESTAMP}.bak"

mkdir -p "${HOST_BACKUP_DIR}"

docker exec "${CONTAINER_NAME}" mkdir -p "${CONTAINER_BACKUP_DIR}"
docker exec "${CONTAINER_NAME}" /bin/bash -lc "
set -euo pipefail
if [ -x /opt/mssql-tools18/bin/sqlcmd ]; then
  SQLCMD=/opt/mssql-tools18/bin/sqlcmd
elif [ -x /opt/mssql-tools/bin/sqlcmd ]; then
  SQLCMD=/opt/mssql-tools/bin/sqlcmd
else
  echo 'sqlcmd was not found in the SQL Server container.' >&2
  exit 1
fi

\"\${SQLCMD}\" -S localhost -U \"${DB_USER}\" -P \"${DB_PASSWORD}\" -C -Q \"
BACKUP DATABASE [${DB_NAME}]
TO DISK = N'${CONTAINER_BACKUP_DIR}/${BACKUP_FILE}'
WITH COPY_ONLY, INIT, COMPRESSION, STATS = 5;\"
"

docker cp "${CONTAINER_NAME}:${CONTAINER_BACKUP_DIR}/${BACKUP_FILE}" "${HOST_BACKUP_DIR}/${BACKUP_FILE}"
printf 'Database backup saved to %s\n' "${HOST_BACKUP_DIR}/${BACKUP_FILE}"
