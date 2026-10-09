# L0N Launcher — nënshkrim digjital si person privat

Ky projekt është për përdorim personal. **Nuk kërkon kompani për zhvillim.** Për ta nënshkruar me reputacion publik në Windows, duhet certifikatë e lëshuar nga një autoritet që është i besuar nga Windows-i.

## Çfarë është gati

- `signing/Sign-L0N-Release.ps1` është **mjet për zhvilluesin**, jo pjesë e Setup-it që u dërgohet përdoruesve.
- Kompilon WPF `win-x64` me .NET të përfshirë.
- Nënshkruan **fillimisht `L0N.GameLauncher.exe`**, pastaj e fut në instalues me Inno Setup.
- Nënshkruan **`L0N_Game_Launcher_Setup.exe` dhe çinstaluesin (uninstaller) të Inno Setup**.
- Përdor SHA-256 dhe timestamp RFC3161; kontrollon zinxhirin e certifikatës dhe të dyja nënshkrimet.
- **Ndërpret procesin** nëse certifikata është self-signed, e pavlefshme, pa çelës privat të aksesueshëm, pa përdorimin "Code Signing", ose verifikimi nuk kalon.
- Nuk merr PIN, pasaportë ose çelës privat në GitHub.

**Status:** Kodi i nënshkrimit është i përgatitur; asnjë version L0N nuk është nënshkruar me certifikatën tënde ende.

## Si merr certifikatën si person privat

1. Hape faqen zyrtare [Certum — Code Signing](https://www.certum.eu/en/code-signing-certificates/).
2. Zgjidh **Standard Code Signing for an individual** (jo certifikatë SSL/TLS, jo certifikatë për dokumente PDF). Certum e dokumenton këtë si të mundshme për individë; **kontrollo disponueshmërinë për Kosovë përpara pagesës**.
3. Verifiko identitetin personal sipas udhëzimeve të Certum. Mund të kërkojnë ID ose pasaportë dhe dokument adrese. Dokumentet dorëzohen **vetëm te ofruesi**, jo në GitHub ose ChatGPT.
4. Zgjidh token fizik ose shërbim cloud me integrim të njohur nga Windows Certificate Store / CryptoAPI. Instalo driverët/klientin sipas ofruesit.
5. Sigurohu që certifikata të shfaqet te `certmgr.msc > Personal > Certificates` me **Code Signing** dhe çelësin privat të disponueshëm. Merre `Thumbprint` nga detajet e saj.

*Microsoft Artifact Signing Public Trust aktualisht kërkon që zhvilluesit individualë të jenë në SHBA ose Kanada.* Për certifikatë individuale nga Kosova, mos e blej shërbimin Microsoft pa konfirmuar eligjibilitetin.

## Nënshkrimi dhe ndërtimi në PC të autorizuar Windows

Në kompjuterin tënd të zhvillimit, instalo:

- Visual Studio 2022 / .NET 8 SDK me Windows Desktop
- Windows 10/11 SDK, ku gjendet Microsoft `signtool.exe`
- Inno Setup 6
- Mjetet e certifikatës të ofruesit (token ose cloud KSP)

Nga PowerShell i zhvillimit, **vetëm pasi ke certifikatën reale**:

```powershell
cd launcher-wpf
.\signing\Sign-L0N-Release.ps1 -CertificateThumbprint "SHKRUJE_THUMBPRINT_40_SHIFRASH_HEX"
```

Nëse certifikata instalohet në `LocalMachine`, përdor `-CertificateStore LocalMachine`. Mjeti mund të kërkojë autorizim/PIN te programi i ofruesit kur nënshkruan. **Mos ia dërgo askujt PIN-in**.

Rezultati pas suksesit:
- `publish/L0N.GameLauncher.exe` — app i nënshkruar
- `dist/L0N_Game_Launcher_Setup.exe` — installer i nënshkruar

Shiko *Properties → Digital Signatures* për të dy skedarët. Emri i botuesit në certifikatë do të jetë emri yt i verifikuar personal, edhe nëse emri i aplikacionit mbetet L0N.

## Siguria

- Një certifikatë **self-signed nuk krijon besim publik** dhe nuk e zgjidh Smart App Control.
- Nënshkrimi nuk garanton leje në PC pune: administratorët IT mund të kenë politika të tjera.
- Mos çaktivizo Smart App Control për testimin e L0N.
- Mos ngarko `.pfx`, PIN, çelësa privatë, pasaportë ose dokumente personale në repository publik.
- Workflow-i i përditshëm në GitHub publikon artefakte **UNSIGNED**. Ekziston edhe workflow-i i veçantë manual `Sign L0N Native Windows Release`, por ai kërkon një PFX legjitim dhe secrets brenda environment-it `code-signing`; **nuk është aktivizuar me certifikatën tënde**.
- Certifikatat e reja me hardware token/cloud HSM shpesh **nuk eksportohen si PFX**. Për to përdor mjetin lokal të nënshkrimit ose konfigurimin e ofruesit; **mos tentoni të eksportoni çelësin privat**.
- Mjeti lokal aktivizon edhe `SignedUninstaller=yes` në Inno Setup për nënshkrimin e çinstaluesit, jo vetëm `Setup.exe`.

## Burime zyrtare

- Certum individual requirements: https://support.certum.eu/en/code-signing-required-documents/
- Certum signing certificates: https://www.certum.eu/en/code-signing-certificates/
- Microsoft Smart App Control: https://learn.microsoft.com/windows/apps/develop/smart-app-control/overview
- Microsoft Artifact Signing: https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart
