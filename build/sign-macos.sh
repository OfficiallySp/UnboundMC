#!/usr/bin/env bash
# Code-sign and notarize a macOS binary. Invoked by CI only when the MACOS_* secrets are set.
#
# NOTE: a bare Mach-O binary can be signed and notarized, but it CANNOT be "stapled"
# (stapling requires a .app / .dmg / .pkg). Without a stapled ticket, Gatekeeper validates
# the notarization online on first launch. For a fully offline first-run experience, wrap
# Unbound in a .app bundle and notarize a .dmg — see SIGNING.md.
#
# Required env (from CI secrets):
#   MACOS_CERTIFICATE_P12       base64 of the Developer ID Application .p12
#   MACOS_CERTIFICATE_PASSWORD  password for that .p12
#   MACOS_SIGN_IDENTITY         e.g. "Developer ID Application: Your Name (TEAMID)"
#   MACOS_NOTARY_KEY            base64 of the App Store Connect API key (.p8)
#   MACOS_NOTARY_KEY_ID         the key id
#   MACOS_NOTARY_ISSUER         the issuer uuid
set -euo pipefail

BIN="${1:?usage: sign-macos.sh <binary>}"
TMP="${RUNNER_TEMP:-/tmp}"
KEYCHAIN="$TMP/unbound-signing.keychain-db"
KPASS="$(uuidgen)"

# Import the signing identity into a throwaway keychain.
security create-keychain -p "$KPASS" "$KEYCHAIN"
security set-keychain-settings -lut 21600 "$KEYCHAIN"
security unlock-keychain -p "$KPASS" "$KEYCHAIN"
echo "$MACOS_CERTIFICATE_P12" | base64 --decode > "$TMP/cert.p12"
security import "$TMP/cert.p12" -k "$KEYCHAIN" -P "$MACOS_CERTIFICATE_PASSWORD" -T /usr/bin/codesign
security set-key-partition-list -S apple-tool:,apple: -k "$KPASS" "$KEYCHAIN" >/dev/null
security list-keychains -d user -s "$KEYCHAIN" $(security list-keychains -d user | sed s/\"//g)

# Sign with hardened runtime + secure timestamp.
codesign --force --options runtime --timestamp --sign "$MACOS_SIGN_IDENTITY" "$BIN"
codesign --verify --strict --verbose=2 "$BIN"

# Notarize (App Store Connect API key).
echo "$MACOS_NOTARY_KEY" | base64 --decode > "$TMP/AuthKey.p8"
ZIP="$TMP/Unbound-notarize.zip"
ditto -c -k --keepParent "$BIN" "$ZIP"
xcrun notarytool submit "$ZIP" \
  --key "$TMP/AuthKey.p8" \
  --key-id "$MACOS_NOTARY_KEY_ID" \
  --issuer "$MACOS_NOTARY_ISSUER" \
  --wait

echo "Signed + notarized: $BIN"
echo "(Bare binaries can't be stapled — Gatekeeper verifies online on first launch. See SIGNING.md.)"
