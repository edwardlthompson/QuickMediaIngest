# Runbook: PreferAdb, OP13 & Delete-After-Import Failure Modes

## 1. PreferAdb Device Discovery Failures

- **Symptom:** ADB device not listed in sources despite USB cable connected.
- **Root Causes & Remediation:**
  1. **USB Debugging Disabled:** Enable Developer Options on Android and verify USB Debugging is ON.
  2. **Unauthorized Host:** Check phone screen for RSA fingerprint authorization prompt.
  3. **ADB Server Deadlock:** Run `adb kill-server && adb start-server` or click Rescan in Quick Media Ingest.

## 2. OnePlus 13 (OP13) FTP/ADB Dual-Mount Aliases

- **Symptom:** Ghost files or duplicate listings across Wi-Fi FTP and ADB USB cable.
- **Remediation:**
  - Quick Media Ingest automatically applies `FtpAdbAliasFilter.DeduplicateDualFtpAliases()`.
  - When PreferAdb is active, physical ADB connections take priority over FTP virtual endpoints.

## 3. Delete-After-Import Safety Protocol

- **Safety Invariant:** Files are NEVER deleted from source media until full checksum/size verification confirms successful write to target.
- **Removable Media Eject Interlock:** If unimported or uncopied files remain on the source card/device, auto-eject pauses with an informative status message to prevent accidental card removal.
