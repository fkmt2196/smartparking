#!/usr/bin/env bash
# Compatible with the Bash 3.2 included with macOS. No local PostgreSQL needed.
set +x
set -euo pipefail

parking_repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$parking_repo_root"

# Shared endpoint from the team's Neon Connect screen; credentials stay local.
parking_host='ep-wispy-bread-aoyxxn50.c-2.ap-southeast-1.aws.neon.tech'
parking_database='smartparking'
parking_username='smartparking_owner'
parking_run_backend=false

while [[ $# -gt 0 ]]; do
    case "$1" in
        --run) parking_run_backend=true; shift ;;
        --username)
            if [[ $# -lt 2 || -z "$2" ]]; then
                printf 'Thieu username sau --username.\n' >&2
                exit 1
            fi
            parking_username="$2"
            shift 2
            ;;
        *) printf 'Cach dung: bash scripts/connect-neon-macos.sh [--run] [--username TEN_USER]\n' >&2; exit 1 ;;
    esac
done

if ! command -v dotnet >/dev/null 2>&1; then
    printf 'Can cai .NET SDK 10 cho macOS truoc khi chay.\n' >&2
    exit 1
fi

cleanup() {
    unset parking_password parking_quoted_password parking_connection parking_result parking_data parking_json
    unset ConnectionStrings__Parking
}
trap cleanup EXIT

printf 'DB chung: %s / %s (branch production)\n' "$parking_host" "$parking_database"
printf 'Nhap mat khau DB Neon cho %s (ky tu duoc che): ' "$parking_username" >&2
IFS= read -r -s parking_password
printf '\n' >&2
if [[ -z "$parking_password" ]]; then
    printf 'Mat khau DB khong duoc de trong.\n' >&2
    exit 1
fi

# Npgsql quotes values containing semicolons; double embedded double quotes.
parking_quoted_password="${parking_password//\"/\"\"}"
parking_quoted_username="${parking_username//\"/\"\"}"
parking_connection="Host=$parking_host;Port=5432;Database=$parking_database;Username=\"$parking_quoted_username\";Password=\"$parking_quoted_password\";SSL Mode=Require;Channel Binding=Require;Timeout=15;Command Timeout=15"
export ConnectionStrings__Parking="$parking_connection"

dotnet tool restore
dotnet build backend/src/Parking.WebAPI --nologo -m:1 -p:UseSharedCompilation=false --verbosity minimal

# EF can exit zero when its DB query fails. Require a known applied true/false
# result from its JSON data channel, never just the process exit code.
if ! parking_result="$(dotnet ef migrations list \
    --project backend/src/Parking.Persistence \
    --startup-project backend/src/Parking.WebAPI \
    --no-build --json --prefix-output --no-color 2>&1)"; then
    printf 'Kiem tra DB that bai. Kiem tra SDK, dependency va cau hinh du an.\n' >&2
    exit 1
fi

parking_data="$(printf '%s\n' "$parking_result" | sed -n 's/^data:[[:space:]]*//p')"
if [[ -z "$parking_data" ]] ||
    grep -Eq '"applied"[[:space:]]*:[[:space:]]*null' <<< "$parking_data" ||
    ! grep -Eq '"applied"[[:space:]]*:[[:space:]]*(true|false)' <<< "$parking_data"; then
    if grep -Eq '28P01|password authentication failed' <<< "$parking_result"; then
        printf 'Sai username hoac mat khau DB Neon. Xin lai thong tin tu nguoi phu trach.\n' >&2
    else
        printf 'Chua xac minh duoc DB. Kiem tra mang, quyen truy cap va thong tin Neon voi nguoi phu trach.\n' >&2
    fi
    exit 1
fi

printf 'DA XAC MINH KET NOI DB NEON CUA NHOM: %s\n' "$parking_database"
if grep -Eq '"applied"[[:space:]]*:[[:space:]]*false' <<< "$parking_data"; then
    printf 'Co migration chua ap dung hoac lich su da reset. Bao nguoi phu trach; script khong tao bang.\n'
fi

# Escape JSON in Bash so no Python/jq installation is needed. Pass the secret
# through stdin to dotnet instead of exposing it in process arguments.
json_escape() {
    local parking_value="$1"
    parking_value="${parking_value//\\/\\\\}"
    parking_value="${parking_value//\"/\\\"}"
    parking_value="${parking_value//$'\n'/\\n}"
    parking_value="${parking_value//$'\r'/\\r}"
    parking_value="${parking_value//$'\t'/\\t}"
    parking_value="${parking_value//$'\b'/\\b}"
    parking_value="${parking_value//$'\f'/\\f}"
    printf '%s' "$parking_value"
}

parking_json="$(json_escape "$parking_connection")"
if ! printf '{"ConnectionStrings:Parking":"%s"}\n' "$parking_json" |
    dotnet user-secrets set --project backend/src/Parking.WebAPI >/dev/null 2>&1; then
    printf 'Da ket noi DB nhung chua luu duoc User Secrets tren may nay.\n' >&2
    exit 1
fi

printf 'Da luu User Secrets. Mat khau khong nam trong repo.\n'
unset ConnectionStrings__Parking
if [[ "$parking_run_backend" == true ]]; then
    dotnet run --project backend/src/Parking.WebAPI --launch-profile http --no-build
else
    printf 'Chay backend: env -u ConnectionStrings__Parking dotnet run --project backend/src/Parking.WebAPI --launch-profile http\n'
fi
