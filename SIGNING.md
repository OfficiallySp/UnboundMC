# Signing & notarization

The release pipeline (`.github/workflows/release.yml`) builds installers for all platforms on
every run. **Signing is optional and stays off until you add the secrets below** — until then the
binaries are produced unsigned and users get the OS warnings described in *End-user experience*.

Cutting a release: push a tag.

```bash
git tag v1.0.0 && git push origin v1.0.0
```

That builds + signs (if configured) + attaches the binaries to a GitHub Release. A plain
`workflow_dispatch` (or branch push) does the same build but uploads to workflow artifacts only.

---

## Windows — Azure Trusted Signing

The cheapest turnkey option (no hardware token). You need an Azure subscription with a **Trusted
Signing** account + certificate profile, and an app registration (service principal) with the
*Trusted Signing Certificate Profile Signer* role.

Add these repo **Actions secrets**:

| Secret | Value |
|---|---|
| `AZURE_TENANT_ID` | Entra tenant id |
| `AZURE_CLIENT_ID` | service-principal app id |
| `AZURE_CLIENT_SECRET` | service-principal secret |
| `TRUSTED_SIGNING_ENDPOINT` | e.g. `https://eus.codesigning.azure.net/` |
| `TRUSTED_SIGNING_ACCOUNT` | Trusted Signing account name |
| `TRUSTED_SIGNING_PROFILE` | certificate profile name |

Once `AZURE_CLIENT_ID` is present, the Windows job signs `Unbound.exe` automatically.

> Alternative: a **Certum Open Source Code Signing** certificate (cheap, for open-source authors)
> on an HSM, signed via `signtool`/`AzureSignTool`. Swap the signing step accordingly.

## macOS — Developer ID + notarization

Needs an **Apple Developer Program** membership (~$99/yr).

1. Create a **Developer ID Application** certificate, export it as `.p12`, then
   `base64 -i cert.p12 | pbcopy`.
2. Create an **App Store Connect API key** (`.p8`), note its **Key ID** and **Issuer ID**, then
   base64-encode the `.p8`.

Add these repo **Actions secrets**:

| Secret | Value |
|---|---|
| `MACOS_CERTIFICATE_P12` | base64 of the Developer ID Application `.p12` |
| `MACOS_CERTIFICATE_PASSWORD` | password for that `.p12` |
| `MACOS_SIGN_IDENTITY` | `Developer ID Application: Name (TEAMID)` |
| `MACOS_NOTARY_KEY` | base64 of the App Store Connect `.p8` |
| `MACOS_NOTARY_KEY_ID` | API key id |
| `MACOS_NOTARY_ISSUER` | API issuer uuid |

The macOS job then runs `build/sign-macos.sh` (codesign with hardened runtime → notarize).

> **Production note:** a bare single-file binary can be signed + notarized but **not stapled**
> (stapling needs `.app`/`.dmg`/`.pkg`). For a clean offline first-run, wrap `Unbound` in a `.app`
> bundle and notarize a `.dmg`. That packaging step is a worthwhile follow-up.

## Linux

No signing required. (Optional: ship an AppImage and/or a detached GPG signature + checksums.)

---

## End-user experience (while unsigned)

- **Windows / SmartScreen:** "Windows protected your PC" → **More info** → **Run anyway**.
- **macOS / Gatekeeper:** right-click the binary → **Open** → **Open**; or
  `xattr -d com.apple.quarantine ./Unbound`.
- **Linux:** `chmod +x ./Unbound && ./Unbound`.

Signing removes the Windows warning and (with notarization) the macOS block. Publishing checksums
(e.g. `sha256sum`) alongside releases is good practice regardless.
