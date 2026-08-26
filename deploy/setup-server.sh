#!/usr/bin/env bash
# Jednorazova priprava cerstveho Ubuntu serveru (napr. Oracle Cloud Free Tier VM)
# pro beh NFC domacnosti. Spustit jako root/sudo.
set -euo pipefail

echo "== ASP.NET Core 9 runtime =="
wget "https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb" -O /tmp/packages-microsoft-prod.deb
dpkg -i /tmp/packages-microsoft-prod.deb
apt-get update
apt-get install -y aspnetcore-runtime-9.0

echo "== Caddy (automaticke HTTPS) =="
apt-get install -y debian-keyring debian-archive-keyring apt-transport-https curl
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' | tee /etc/apt/sources.list.d/caddy-stable.list
apt-get update
apt-get install -y caddy

echo "== Systemovy ucet a slozky appky =="
useradd --system --no-create-home --shell /usr/sbin/nologin nfchome || true
mkdir -p /opt/nfc-home-manager/App_Data
chown -R nfchome:nfchome /opt/nfc-home-manager

echo "== Lokalni firewall =="
ufw allow 80/tcp || true
ufw allow 443/tcp || true

cat <<'EOF'

Hotovo. Dalsi kroky:
  1. Nahraj obsah publish vystupu (dotnet publish -c Release) do /opt/nfc-home-manager
     (vcetne appsettings.Production.json s vyplnenym PasswordHash a AllowedHosts).
  2. cp deploy/nfc-home-manager.service /etc/systemd/system/
  3. cp deploy/Caddyfile /etc/caddy/Caddyfile
  4. chown -R nfchome:nfchome /opt/nfc-home-manager
  5. systemctl daemon-reload && systemctl enable --now nfc-home-manager
  6. systemctl reload caddy
  7. V Oracle Cloud konzoli: VCN -> Security Lists -> pridat Ingress pravidla
     pro porty 80 a 443 (0.0.0.0/0) - bez toho firewall na urovni cloudu
     blokuje provoz i kdyz ufw/iptables na serveru vypadaji v poradku.
  8. Na Forpsi DNS spravci prepni A zaznam nfc.scitani1921.cz na verejnou IP
     tohoto serveru.
EOF
