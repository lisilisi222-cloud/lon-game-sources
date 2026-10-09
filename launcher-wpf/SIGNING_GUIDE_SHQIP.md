# L0N — Saktësim i nënshkrimit digjital

**Dokumenti kryesor është [SIGNING.md](./SIGNING.md).** Lexoje para se të marrësh certifikatën.

## Procesi aktual në GitHub
- Workflow aktiv: `.github/workflows/sign-l0n-windows.yml` — `Sign L0N Native Windows Release`.
- Ky workflow nis vetëm manualisht, përdor environment `code-signing`, RSA Authenticode SHA-256, timestamp RFC3161, Inno Setup SignTool dhe `SignedUninstaller=yes`.
- Nënshkruan dhe verifikon programin kryesor, instaluesin dhe çinstaluesin në një mjedis Windows. Publikon `L0N-SIGNED-Windows-Setup` vetëm pas suksesit.
- Build-et që kaluan CI më parë janë **UNSIGNED**. Nuk janë certifikatë apo provë reputacioni Windows.

## Çka mungon
**Nuk është konfiguruar një certifikatë publike e besuar e lëshuar për pronarin.** ChatGPT nuk mund të kryejë verifikimin e identitetit te autoriteti lëshues.

Workflow ekzistues kërkon environment secrets `L0N_SIGNING_PFX_BASE64` dhe `L0N_SIGNING_PFX_PASSWORD` te GitHub Settings → Environments → code-signing.
**Kujdes:** Shumë certifikata të reja Code Signing kanë private key në HSM/cloud signer dhe nuk eksportohen si PFX. Në këtë rast duhet përshtatur workflow sipas ofruesit. Mos ngarko kurrë private key në repo publik ose në chat.

Microsoft Azure Artifact Signing është alternativë vetëm për përdorues që kualifikohen për identitetin Public Trust. Sipas dokumentacionit zyrtar (tetor 2026), individët Public Trust duhet të jenë në SHBA/Kanada; Kosova nuk figuron në listën e vendeve të kualifikuara për organizatat. Profilet Private Trust nuk ofrojnë të njëjtin besim publik.
https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart

Nëse je në Kosovë, pyet një autoritet lëshues të certifikatave (p.sh. DigiCert, Sectigo, GlobalSign) nëse mbështet individët ose bizneset e regjistruara në Kosovë dhe çfarë mënyre nënshkrimi/HSM ofron. Konfirmo para pagesës.

## Siguria
- Mos çaktivizo Smart App Control, antivirus ose politikat e IT-së.
- Nënshkrimi i vlefshëm nuk garanton se një PC pune do ta pranojë programin; reputacioni dhe politikat e organizatës ndikojnë ende.
- Asnjë artefakt nuk duhet të quhet SIGNED pa verifikim të vërtetë të nënshkrimit dhe identitetit.

Burimet: [Microsoft Authenticode](https://learn.microsoft.com/en-us/windows/win32/seccrypto/signtool), [Inno Setup SignedUninstaller](https://jrsoftware.org/ishelp/index.php?topic=setup_signeduninstaller), [Microsoft Artifact Signing](https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart).
