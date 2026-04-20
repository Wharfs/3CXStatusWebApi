# 3CXStatusWebApi

Small HTTP service that runs on a 3CX v20 server and exposes three endpoints for querying and flipping extension forwarding profiles. Pairs with [3CXStatusTray](https://github.com/Wharfs/3CXStatusTray), the Windows notification-area applet that gives any desk in a small office a one-click "phones on / phones off" control.

## Why this exists

Small office, around 10 people, about 8 of whom answer phones. Lunch is taken ad-hoc, not on a schedule. The office needs a way for any desk to flip everyone's extensions to Out of Office at lunch and back to Available afterwards, with a shared visual signal so everyone can see the current state at a glance.

3CX v20's XAPI (the modern REST API) doesn't yet support setting a user's `CurrentProfileName` — it's read-only, PATCH silently ignores changes. An [open issue on the 3CX community forums](https://www.3cx.com/community/threads/patch-verb-xapi-v1-users-id-not-working-for-currentprofilename.126355/) has been tracking this for over a year. So the only way to programmatically flip extension profiles on v20 today is via the legacy 3CX Call Control API — a managed .NET assembly (`3cxpscomcpp2.dll`) that ships with the PBX.

This service is a thin HTTP wrapper around that assembly, exposing just enough surface for the tray to:

1. Read the current forwarding profile of a designated sentinel extension (to decide the tray's colour).
2. Set every extension's profile in one call (to toggle the whole office at lunch).

It runs on the 3CX server itself (typically `/opt/3CXWebApi/` on a v20 Debian install) and listens on plain HTTP `:8889` by default.

## How it works

```
┌──────────────────────┐    poll every 5s       ┌──────────────────┐   COM    ┌───────┐
│  Tray (one per desk) │ ---------------------> │ 3CXStatusWebApi  │ -------> │  3CX  │
│  x N desks           │ <--------------------- │   (on PBX host)  │          │  PBX  │
└──────────────────────┘    toggle on click     └──────────────────┘          └───────┘
```

- At startup, reads `3CXPhoneSystem.ini` from its working directory to discover the PBX's ConfService host, port, and credentials.
- Calls `TCXPhoneSystem.Reset(...)` and `WaitForConnect(...)` to establish a session with the 3CX ConfService on `127.0.0.1`.
- Builds an ASP.NET Core + [FastEndpoints](https://fast-endpoints.com/) HTTP service that translates the three HTTP endpoints into calls on the loaded `PhoneSystem`, `Tenant`, and `Extension` types.
- On shutdown, calls `Disconnect()` cleanly.

The `3cxpscomcpp2.dll` itself is loaded at runtime via an `AssemblyResolve` hook that points at the PBX's own `/Bin` directory (derived from `General:AppPath` in the ini). The build-time `<HintPath>` is for compile-time reference resolution only; nothing from 3CX is bundled in the service's install folder.

## Endpoints

| Method | Path | Purpose |
|--------|------|---------|
| `GET` | `/status/extension/{id}` | Current forwarding profile of one extension. |
| `GET` | `/status/extension/{id}/profile/{shortcode}` | Set one extension's profile. |
| `GET` | `/status/extensions/profile/{shortcode}` | Set every extension's profile in one call. |

`shortcode` is one of: `available`, `away`, `out_of_office`, `custom1`, `custom2`. They map to the 3CX-internal profile display names (`Available`, `Away`, `Out of office`, `Custom 1`, `Custom 2`).

Responses are a uniform JSON shape:

```json
{ "message": "Available", "status": "OK", "timeStamp": "2026-04-20T12:30:00Z" }
```

`status` values include `OK`, `NOT_FOUND`, `BAD_REQUEST` — enough for clients to tell success from a missing-extension error from a bad short-code.

### Authentication

All endpoints are `AllowAnonymous` by default. To require auth, set `ApiKey` to a non-empty value in `appsettings.json`. Once set, all requests must carry an `X-API-Key` header matching that value, or they'll get a 401. Leave `ApiKey` empty (the default) for no auth on a trusted LAN.

## Prerequisites

- 3CX v20 Phone System. Tested against the standard v20 Debian install; should also work on 3CX-for-Windows if that's still an option.
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) for building. The deployed service is published self-contained, so the 3CX server itself does *not* need .NET installed.
- Access to the `3cxpscomcpp2.dll` managed assembly that ships with 3CX — at build time only. On a v20 Debian install it's at `/usr/lib/3cxpbx/3cxpscomcpp2.dll` (system-shared, updated on PBX upgrades).

## Building

Cross-platform — the tray half is Windows-only, but this service isn't:

```bash
# On the 3CX server (default PbxBinPath already correct):
dotnet build -c Release

# On any Linux dev box, with the DLL copied to ~/lib/3cx/:
dotnet build -c Release -p:PbxBinPath=/home/you/lib/3cx

# On Windows with a legacy 3CX-for-Windows install:
dotnet build -c Release
```

`PbxBinPath` has per-OS defaults (Linux: `/var/lib/3cxpbx/Instance1/Bin`, Windows: `C:\Program Files\3CX Phone System\Bin`). Override at build time if your install lives elsewhere — commonly, the `/usr/lib/3cxpbx/` copy on a Debian server.

### Tests

```bash
dotnet test
```

Covers the profile short-code mapping, the `OVERRIDEOFFICETIME` system-status mapping, and the optional API-key middleware — everything that doesn't need a running 3CX instance. The parts that *do* hit 3CX (the actual `PhoneSystem` calls) aren't unit-tested; they're validated by manual `curl` against a real PBX during deploy.

## Deploying

### Publish a self-contained tarball

Run on any .NET-8-SDK-equipped machine (doesn't have to be the 3CX server). Produces a standalone bundle that ships its own .NET 8 runtime, so the target server doesn't need .NET installed at all.

```bash
dotnet publish 3CXStatusWebApi/3CXStatusWebApi.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PbxBinPath=/path/to/folder/containing/3cxpscomcpp2.dll \
  -o /tmp/webapi-publish

tar czf /tmp/webapi.tgz -C /tmp/webapi-publish .
```

Output is ~40 MB compressed, ~95 MB uncompressed. Transfer to the 3CX server alongside `tools/deploy-webapi.sh`:

```bash
scp /tmp/webapi.tgz tools/deploy-webapi.sh root@<3cx>:/root/
```

### Install or upgrade on the 3CX server

`tools/deploy-webapi.sh` handles both fresh installs and upgrades in one script — it detects which is which by looking for an existing install dir and a registered systemd unit.

```bash
# On the 3CX Debian server, as root:
sudo ./deploy-webapi.sh /root/webapi.tgz
```

**Fresh install:** creates `/opt/3CXWebApi/`, extracts the tarball, symlinks `3CXPhoneSystem.ini` from its usual PBX location, writes the systemd unit to `/etc/systemd/system/3CXWebApi.service`, enables it, and starts it.

**Upgrade:** stops the running service, backs up the current install to `/opt/3CXWebApi.backup-<date>`, preserves `3CXPhoneSystem.ini` (whether it's a real file or a symlink), replaces contents with the new tarball, restores the ini, and restarts. A rollback command is printed at the end of the script for easy reversion if something goes wrong.

Both paths finish with a `GET /status/extension/100` curl smoke test. Exit code 2 means something went wrong — check `journalctl -u 3CXWebApi -n 50` for the stack trace.

## Configuration

`appsettings.json` at `/opt/3CXWebApi/appsettings.json`:

```json
{
  "PbxIniPath": "3CXPhoneSystem.ini",
  "Urls": "http://*:8889",
  "ApiKey": "",
  "Logging": { "LogLevel": { "Default": "Information" } }
}
```

| Key | Purpose |
|-----|---------|
| `PbxIniPath` | Path to the 3CX ini file. Defaults to `3CXPhoneSystem.ini` in the service's working directory. |
| `Urls` | ASP.NET Core's standard binding key. Override to change port or bind address. |
| `ApiKey` | If non-empty, requires an `X-API-Key: <value>` header on every request. |
| `Logging` | Standard `Microsoft.Extensions.Logging` config. |

After editing, restart: `systemctl restart 3CXWebApi`.

## Cutting a release

Releases aren't automated via GitHub Actions — the 3CX `3cxpscomcpp2.dll` is a 3CX-licensed assembly, not redistributable on public CI, and a stub reference assembly is more maintenance than it saves given how rarely this releases.

Manual release flow, run from any .NET-8-SDK-equipped machine with the DLL available:

```bash
# 1. Bump the version (edit wherever you want a single source of truth; there isn't one right now)

# 2. Publish
dotnet publish 3CXStatusWebApi/3CXStatusWebApi.csproj \
  -c Release -r linux-x64 --self-contained true \
  -p:PbxBinPath=/path/to/dll-folder \
  -o /tmp/webapi-publish
tar czf /tmp/webapi-<version>.tgz -C /tmp/webapi-publish .

# 3. Tag and push
git tag v<version>
git push origin v<version>

# 4. Attach the tarball and deploy script to the release
gh release create v<version> /tmp/webapi-<version>.tgz tools/deploy-webapi.sh \
  --title "WebApi v<version>" --generate-notes
```

Anyone with a 3CX v20 server can then download the tarball + `deploy-webapi.sh` from the Release and stand up a fresh install.

## Credits

Originally adapted from [Montesuma80/3cx-web-API](https://github.com/Montesuma80/3cx-web-API) and examples at [fzany/3CXCallControlAPI_v16](https://github.com/fzany/3CXCallControlAPI_v16). Thanks to both.

Uses [FastEndpoints](https://fast-endpoints.com/) for the HTTP layer.

## License

MIT. See `LICENSE.md`.
