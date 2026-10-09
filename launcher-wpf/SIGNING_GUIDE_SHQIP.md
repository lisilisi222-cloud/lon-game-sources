# L0N Game Launcher — nënshkrimi digjital në Windows

## Gjendja
- Build-et e mëparshme të L0N janë **UNSIGNED**.
- Workflow i ri `Sign L0N Windows (Azure Artifact Signing)` ka `preflight` dhe `sign`.
- `preflight` vetëm kompilon/teston. **Nuk është nënshkrim digjital**.
- `sign` përdor Microsoft Azure Artifact Signing dhe publikon artefakte `SIGNED` vetëm kur **si EXE ashtu edhe Setup.exe** verifikohen me SignTool dhe testi i instalimit kalon.
- Uninstaller-i i Inno Setup aktualisht nuk është i nënshkruar; duhet konfigurim i veçantë i `SignedUninstaller` për nënshkrim në kohën e kompilimit.

## Duhet identitet i verifikuar
Certifikata e besuar publike **nuk mund të krijohet nga ChatGPT**. Lëshohet nga autoritet i besuar pas verifikimit të botuesit.

**Microsoft Azure Artifact Signing Public Trust (tetor 2026):** Microsoft kufizon vendet ku ofrohet. Individët duhet të jenë në SHBA ose Kanada; organizatat në një grup tjetër vendesh të listuara. Kosova nuk figuron në listë. Një certifikatë `Private Trust` nuk i zëvendëson certifikatat e besuara publikisht në Windows.
https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart

Nëse nuk je i kualifikuar për Azure Public Trust, pyet DigiCert, Sectigo ose GlobalSign nëse mund të lëshojnë **Code Signing** për identitetin/juridiksionin tënd. Konfirmo pranueshmërinë përpara pagesës.

## Nëse ke llogari Azure Artifact Signing Public Trust
1. Krijo llogarinë, përfundo identity validation dhe krijo profilin `Public Trust`.
2. Krijo Entra App Registration dhe Federated Identity Credential (GitHub OIDC, branch `main`), dhe jepi aplikacionit rolin **Artifact Signing Certificate Profile Signer** për profilin.
3. Shko në GitHub repository → Settings → Secrets and variables → Actions. Fut këto vlera **vetëm në GitHub**, jo në bisedë:

**Repository secrets**:
- `L0N_AZURE_CLIENT_ID`
- `L0N_AZURE_TENANT_ID`
- `L0N_AZURE_SUBSCRIPTION_ID`

**Repository variables**:
- `L0N_SIGNING_ENDPOINT` — endpoint i rajonit të llogarisë tënde, p.sh. `https://eus.codesigning.azure.net/` vetëm si shembull
- `L0N_SIGNING_ACCOUNT` — emri i llogarisë tënde Azure Artifact Signing
- `L0N_CERT_PROFILE` — emri i profilit të certifikatës Public Trust

4. Hap GitHub → Actions → **Sign L0N Windows (Azure Artifact Signing)** → Run workflow → `sign`. Duhet main branch.
5. Workflow verifikon EXE, Setup.exe dhe EXE të instaluar (`signtool verify /pa /all /v /tw`). Artefaktet `SIGNED` krijohen vetëm kur të gjitha testet kalojnë.

## Nënshkrimi me certifikatë tjetër
Për certifikatë Code Signing të një CA-je tjetër me HSM/token ose remote signer, përdor SignTool sipas dokumentimit të provider-it. Shembull **vetëm kur certifikata është instaluar e aksesueshme**:

```powershell
signtool sign /a /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 'L0N.GameLauncher.exe'
signtool verify /pa /all /v /tw 'L0N.GameLauncher.exe'
# Pastaj kompilo Inno Setup me EXE-në tashmë të nënshkruar.
signtool sign /a /fd SHA256 /tr http://timestamp.digicert.com /td SHA256 'L0N_Game_Launcher_Setup.exe'
signtool verify /pa /all /v /tw 'L0N_Game_Launcher_Setup.exe'
```

Për uninstaller-in e Inno Setup, shiko dokumentimin për `SignedUninstaller` dhe `SignTool`; nënshkrimi i installerit pas kompilimit nuk nënshkruan automatikisht uninstaller-in.

## Kujdes
- Asnjë certifikatë, çelës privat, PFX, password ose token nuk duhet futur në GitHub source ose në ChatGPT.
- Mos e çaktivizo Smart App Control/antivirusin për të instaluar aplikacionin.
- Edhe një aplikacion i nënshkruar mund të bllokohet nga Smart App Control / SmartScreen ose nga politikat e kompjuterit të punës.
- Nënshkrimi dhe vulosja kohore SHA-256 nuk janë provë që programi nuk ka gabime.

Burime: https://github.com/Azure/artifact-signing-action , https://learn.microsoft.com/en-us/windows/win32/seccrypto/using-signtool-to-verify-a-file-signature , https://jrsoftware.org/ishelp/
