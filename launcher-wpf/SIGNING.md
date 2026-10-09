# L0N Game Launcher — nënshkrimi digjital i besuar

## Gjendja

Aplikacioni L0N është projekt **Windows C# / WPF**, me instalues Inno Setup. Ndërtimi normal (i panënshkruar) kaloi testet automatike të Windows-it në tetor 2026.

U shtua një workflow i veçantë:

- `.github/workflows/sign-l0n-windows.yml`
- GitHub → **Actions → Sign L0N Native Windows Release → Run workflow**
- Nis **vetëm manualisht**; nuk përdoret për të nënshkruar automatikisht çdo commit.
- Ndërton programin, provon testin C#, nënshkruan aplikacionin me **RSA / SHA-256** dhe timestamp, krijon **Setup dhe Uninstaller të nënshkruar**, e instalon në Windows dhe verifikon nënshkrimet.
- Publikon artefaktin **L0N-SIGNED-Windows-Setup** vetëm nëse të gjitha hapat kalojnë.
- Nuk publikon asnjë artefakt me etiketën "SIGNED" nëse certifikata mungon ose verifikimi dështon.

### Çka është e nevojshme prej pronarit të projektit

**Nuk ka ende certifikatë të besuar të lidhur me repository-n.** ChatGPT nuk mund të lëshojë certifikatë në emrin tënd ose të kryejë verifikim identiteti në vendin tënd.

1. Merr një certifikatë **RSA code signing** të lëshuar nga një autoritet i besuar në Microsoft Trusted Root Program, në emrin ligjor të pronarit ose biznesit. Pyet ofruesin për formatin e ruajtjes së çelësit **përpara blerjes**.
2. Workflow aktual mbështet një certifikatë **PFX/PKCS#12 me çelës privat që mund të importohet**. Shumë certifikata të reja ofrohen vetëm përmes HSM ose cloud-signing dhe **nuk** eksportohen si PFX. Në atë rast duhet integruar API/tool-i i ofruesit, jo ky workflow.
3. Hape repo-n në GitHub → **Settings → Environments** dhe krijo environment `code-signing`. Vendos **Required reviewers** nëse të lejon plani i GitHub-it.
4. Te environment `code-signing`, shto dy **Secrets**:
   - `L0N_SIGNING_PFX_BASE64` — kodimi Base64 i PFX-it privat (jo certifikata publike vetëm).
   - `L0N_SIGNING_PFX_PASSWORD` — fjalëkalimi i PFX-it.
5. Për PFX në kompjuterin personal Windows, pas marrjes së certifikatës, mund ta kodosh në clipboard pa e ruajtur në një skedar tekst:
   ```powershell
   [Convert]::ToBase64String([IO.File]::ReadAllBytes("C:\Private\L0N-Codesign.pfx")) | Set-Clipboard
   ```
   Pastaj ngjite vlerën te secret-i përkatës në GitHub. **Mos e dërgo PFX-in, fjalëkalimin ose Base64 te ChatGPT, mesazhe ose repository publik.**
6. Te **Actions → Sign L0N Native Windows Release → Run workflow**, nis ndërtimin. Nëse GitHub kërkon aprovimin e environment-it, aprovoje vetëm pasi të kesh kontrolluar ndryshimet në kod.
7. Kur workflow të japë **Success**, hape run-in dhe shkarko `L0N-SIGNED-Windows-Setup` nga **Artifacts**. Kontrollo vetë botuesin: Properties → Digital Signatures.

### Çka nuk duhet bërë

- Mos e fik **Smart App Control**, mos bëj registry-bypass dhe mos përdor certifikata të rreme për të kaluar bllokimet.
- Një certifikatë **self-signed** falas nuk bën që programi të njihet si i besuar nga Smart App Control.
- Nënshkrimi i vlefshëm nuk garanton që kompjuteri i punës do ta lejojë: politikat e IT-së mund të kërkojnë aprovime të tjera.
- Emri që shfaq Windows si botues vjen nga **identiteti i certifikatës së verifikuar**, jo domosdoshmërisht nga emri i markës `L0N`.
- Mund të kërkohet aprovimi i departamentit IT, edhe me nënshkrim të vlefshëm.

### Nëse certifikata është në hardware token/cloud HSM

**Mos** u përpiq ta eksportosh çelësin privat. Më trego vetëm emrin e ofruesit të nënshkrimit (jo token/key/password), dhe mund të përshtatim workflow për komandat/API-të e atij ofruesi.

### Burime

- Microsoft — Smart App Control dhe nënshkrimi: https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/code-signing-for-smart-app-control
- Microsoft — SignTool: https://learn.microsoft.com/en-us/windows/win32/seccrypto/signtool
- Microsoft — code signing options: https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options
- Inno Setup — SignTool/SignedUninstaller: https://jrsoftware.org/ishelp/index.php?topic=setup_signtool
