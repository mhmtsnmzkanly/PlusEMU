# PlusEMU Systemd Deployment Guide

## Scope

This guide explains how to publish PlusEMU for Linux, deploy it under a dedicated service account, and run it through `systemd`.

The instructions assume:
- Linux host
- `linux-x64` target
- self-contained publish output
- deployment path `/opt/plusemu`
- service user `plusemu`

## 1. Build The Publish Output

From the project root:

```bash
DOTNET_ROOT=/usr/share/dotnet PATH=/usr/share/dotnet:$PATH /usr/share/dotnet/dotnet publish -c Release -r linux-x64 --self-contained true
```

Expected output:

```text
bin/Release/net10.0/linux-x64/publish/
```

Main executable:

```text
bin/Release/net10.0/linux-x64/publish/Plus Emulator
```

## 2. Create A Dedicated Service User

Run as root:

```bash
sudo useradd --system --home /opt/plusemu --shell /usr/sbin/nologin plusemu
```

If the user already exists, keep it.

## 3. Prepare The Deployment Directory

```bash
sudo mkdir -p /opt/plusemu
sudo chown -R plusemu:plusemu /opt/plusemu
```

## 4. Copy The Publish Output

Copy the published files into `/opt/plusemu`:

```bash
sudo rsync -av --delete "bin/Release/net10.0/linux-x64/publish/" /opt/plusemu/
sudo chown -R plusemu:plusemu /opt/plusemu
```

## 5. Verify Required Runtime Files

Before starting the service, confirm these paths exist under `/opt/plusemu`:

- `Plus Emulator`
- `Config/config.json`
- `Config/nlog.config`
- `Config/figuredata.xml`
- `Config/Revisions/`
- `plugins/` if you use plugins

The application uses its working directory to locate `Config`, `plugins`, and related runtime assets. `WorkingDirectory` in `systemd` must point at the deployed root.

## 6. Create The systemd Unit

Create:

```text
/etc/systemd/system/plusemu.service
```

Recommended unit:

```ini
[Unit]
Description=PlusEMU
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
User=plusemu
Group=plusemu
WorkingDirectory=/opt/plusemu
ExecStart=/opt/plusemu/Plus\ Emulator
Restart=always
RestartSec=5
KillSignal=SIGINT
TimeoutStopSec=30

[Install]
WantedBy=multi-user.target
```

## 7. Reload And Start The Service

```bash
sudo systemctl daemon-reload
sudo systemctl enable plusemu
sudo systemctl start plusemu
```

Check status:

```bash
sudo systemctl status plusemu
```

Follow logs:

```bash
sudo journalctl -u plusemu -f
```

## 8. File Permissions

At minimum, the service user must be able to:

- read everything under `/opt/plusemu`
- execute `/opt/plusemu/Plus Emulator`
- write to any directories configured in `nlog.config`

If NLog writes to files under the application tree, create those directories first and give ownership to `plusemu`.

Example:

```bash
sudo mkdir -p /opt/plusemu/logs
sudo chown -R plusemu:plusemu /opt/plusemu/logs
```

## 9. Configuration Checklist

Before production start, validate:

- database host, port, name, username, and password in `Config/config.json`
- Flash/Nitro bind addresses and ports
- RCON settings
- firewall rules for the ports you expose
- plugin binaries, if any, inside `/opt/plusemu/plugins`

## 10. Updating The Service

Recommended update flow:

1. Build a new publish output.
2. Stop the service.
3. Replace the deployed files.
4. Restore ownership.
5. Start the service again.

Commands:

```bash
DOTNET_ROOT=/usr/share/dotnet PATH=/usr/share/dotnet:$PATH /usr/share/dotnet/dotnet publish -c Release -r linux-x64 --self-contained true
sudo systemctl stop plusemu
sudo rsync -av --delete "bin/Release/net10.0/linux-x64/publish/" /opt/plusemu/
sudo chown -R plusemu:plusemu /opt/plusemu
sudo systemctl start plusemu
sudo systemctl status plusemu
```

If the unit file itself changed:

```bash
sudo systemctl daemon-reload
sudo systemctl restart plusemu
```

## 11. Common Failure Points

### Service starts then exits immediately

Check:

- `sudo journalctl -u plusemu -n 200 --no-pager`
- `/opt/plusemu/Config/config.json`
- database connectivity
- missing `Config/Revisions` files

### `systemd` says executable not found

Check:

- file exists: `/opt/plusemu/Plus Emulator`
- executable bit is set:

```bash
sudo chmod +x "/opt/plusemu/Plus Emulator"
```

### Plugin loading fails

Check:

- plugin DLLs exist under `/opt/plusemu/plugins`
- plugin dependencies were copied
- file ownership allows reads by `plusemu`

### NLog cannot write

Check the output path configured in `Config/nlog.config` and make sure the target directory exists and is writable by `plusemu`.

## 12. Optional Hardening

After the base service works, you can tighten the unit with settings such as:

```ini
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=full
ProtectHome=true
```

Do not enable these blindly. The emulator must still be able to read its runtime files and write its log targets.

## 13. Practical Notes For This Project

- Self-contained publish means you do not need a separate system-installed .NET runtime on the target host.
- The application depends on its deployed working directory layout, not just the executable alone.
- The binary name contains a space: `Plus Emulator`. Keep the escaped path in `ExecStart`, or rename the binary during deployment if you prefer a simpler service file.
