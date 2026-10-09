# L0N Game Launcher — Code Signing (Windows)

**Status: no CA-issued trusted certificate has been supplied. All current Windows build artifacts remain UNSIGNED.** Do not describe any artifact as signed until the complete protected release job succeeds.

## Two separate workflows

- `.github/workflows/build-l0n-windows.yml` runs Windows build / startup / Inno Setup tests and uploads artifacts explicitly named `UNSIGNED`. It needs no certificate.
- `.github/workflows/sign-l0n-windows.yml` is **manual only**, on reviewed `main`, behind the `code-signing` environment approval. It requires a dedicated Windows `self-hosted` runner with label `l0n-codesign` and an approved signer in that Windows account's `CurrentUser\\My` certificate store. It fails closed if any check fails.

The signed workflow signs `L0N.GameLauncher.exe` with RSA/SHA-256 + an RFC3161 timestamp, builds an Inno Setup signed installer and signed embedded uninstaller, installs it, checks that EVERY examined signature has the exact approved thumbprint and timestamp, runs the application self-test and only THEN uploads artifacts named `L0N-SIGNED-...`.

## Before purchasing a certificate

Ask the certificate provider ALL of the following:

1. Do you issue **publicly trusted RSA (not ECC) Code Signing** certificates for a person or registered company in my country, with a legal name I can prove? For Kosovo, confirm regional eligibility directly before paying.
2. What identity-validation documents and fees are required? What legal **Publisher** name will Windows show?
3. Is the certificate chained to a CA recognized by the **Microsoft Trusted Root Program** for code signing (not an internal, developer, SSL, or self-signed certificate)?
4. Is the private key protected in a compliant USB token, cloud HSM or managed signing service? **Is the key non-exportable?** Do not attempt to export it as PFX.
5. Can the provider expose the signing key to `signtool.exe` through a **Windows CSP/KSP**, with the certificate in `Cert:\\CurrentUser\\My` for an unattended Windows runner? Does it require interactive PIN confirmation?
6. If it instead needs a provider-specific API/CLI, obtain the **public documentation/name of the product**, not the key, PIN or credentials; the workflow must be adapted to that provider before release.

Modern publicly trusted code-signing keys generally must remain protected by hardware/HSM or an authorized signing service; do not use the earlier GitHub PFX upload approach for a newly issued non-exportable key.

## Configure a protected runner (only after choosing the provider)

1. Use a **dedicated, locked-down Windows x64 signing machine** / runner account with the vendor's token or HSM-backed Windows key provider installed. Install .NET SDK 8, Windows SDK SignTool and Inno Setup 6.
2. Register a GitHub Actions **self-hosted** Windows x64 runner with the additional label `l0n-codesign`. The certificate's non-exportable private key must be available to THAT runner identity through `Cert:\\CurrentUser\\My`. Token PIN entry may require provider-specific automation.
3. Protect the `main` branch with mandatory review and secure the signing machine. **Do not execute untrusted PR/fork code on this runner.** GitHub warns about the risks of self-hosted runners in public repositories. Prefer isolated/ephemeral infrastructure and runner-group restrictions where available; if those protections cannot be assured, use a provider's managed cloud-signing integration instead.
4. In GitHub **Settings → Environments**, create `code-signing`, restrict it to `main`, enable required reviewers and do not allow self-review when available.
5. In the environment's **Variables** (not Secrets), set `L0N_CERT_THUMBPRINT` to the 40-character hexadecimal thumbprint of the approved public code-signing certificate. This is public metadata, not a secret.
6. Do **not** upload `.pfx`, `.p12`, private keys, PINs, API tokens or passwords to GitHub source, artifacts, logs or ChatGPT. Keep the private key under the provider's protection. The current signing workflow requires **no GitHub PFX secret**.
7. Run **Actions → Sign L0N Native Windows Release → Run workflow** from `main`, approve the protected environment only after checking the source commit, and inspect the job result. If no matching runner is registered, the job cannot start.

## Release verification

A successful signed job verifies the publisher's certificate, certificate lifetime/EKU/RSA, Windows certificate chain, signed executable, installer and installed uninstaller, signer thumbprint, RFC3161 timestamp, installation and startup self-test. `signtool verify /tw` alone does NOT make a missing timestamp fatal, so `Verify-L0N-Authenticode.ps1` checks the timestamp explicitly. SHA-256 hashes are recorded for both final artifacts.

After downloading an artifact from the **successful signed job**, verify locally:

```powershell
Get-AuthenticodeSignature .\L0N_Game_Launcher_Setup.exe | Format-List Status,SignerCertificate,TimeStamperCertificate
signtool verify /pa /all /v /tw .\L0N_Game_Launcher_Setup.exe
```

If either verification fails, do not distribute the file. A valid public signature is not a promise that employer IT policy, Defender SmartScreen or Smart App Control will always approve the app; no registry bypass or deactivation is part of L0N's deployment.

## Related project files

- `launcher-wpf/signing/Verify-L0N-Authenticode.ps1`: strict signature identity and timestamp checks.
- `launcher-wpf/scripts/sign-inno-output.ps1`: signs Inno Setup and embedded uninstaller using the store certificate thumbprint.
- `launcher-wpf/signing/Sign-L0N-Release.ps1`: optional local Windows signing for a token/KSP already installed on your own PC.
- `launcher-wpf/installer/L0N.iss`: `SIGNED_BUILD` enables Inno's `SignTool` and `SignedUninstaller`; normal builds remain unsigned.

Official references:
- [Microsoft Smart App Control signing](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/code-signing-for-smart-app-control)
- [CA/Browser Forum Code Signing Baseline Requirements](https://cabforum.org/working-groups/code-signing/requirements/)
- [Microsoft SignTool](https://learn.microsoft.com/en-us/windows/win32/seccrypto/signtool)
- [Inno Setup SignedUninstaller](https://jrsoftware.org/ishelp/topic_setup_signeduninstaller.htm)
- [GitHub environment protection](https://docs.github.com/en/actions/how-tos/deploy/configure-and-manage-deployments/control-deployments)
