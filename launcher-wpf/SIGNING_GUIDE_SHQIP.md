# L0N — Udhëzues i shkurtër për nënshkrimin digjital

**Statusi: NUK ka certifikatë Code Signing të besuar. Setup.exe dhe L0N.GameLauncher.exe janë ende të panënshkruar.**

Lexo dokumentin kryesor [SIGNING.md](./SIGNING.md) për konfigurimin teknik dhe kufizimet e sigurisë.

## Çka është përgatitur në GitHub?

- **Build L0N Native Windows Setup**: ndërton/teston aplikacionin dhe e publikon si `UNSIGNED`.
- **Sign L0N Native Windows Release**: nis vetëm manualisht nga `main` me environment `code-signing` dhe një runner Windows të dedikuar me label `l0n-codesign`. Nënshkruan aplikacionin, Setup dhe Uninstaller, bën verifikim të identitetit dhe timestamp-it, pastaj publikon artefakte `SIGNED` vetëm nëse kalojnë të gjitha testet.
- Nuk ngarkojmë PFX, private key ose PIN në GitHub. Për rrjedhën aktuale kërkohet certifikatë e vërtetë RSA e lëshuar nga CA e besuar, me çelës në token/HSM ose Windows KSP të ofruesit.

## Çka duhet me siguru ti?

1. Pyet një ofrues të certifikatave Code Signing që **pranon regjistrimin/dokumentet e tua** (nëse je në Kosovë, konfirmoje drejtpërdrejt).
2. Kërko **RSA Code Signing** të besuar publikisht nga Microsoft, jo certifikatë SSL, vetë-nënshkruar ose ECC.
3. Pyete a punon me Windows `signtool.exe` përmes USB token/KSP/HSM, ose a kërkon shërbim API. Kjo vendos cili workflow duhet të përdoret.
4. Për nënshkrim direkt në GitHub duhet një Windows runner i dedikuar, environment me aprovim, `main` i mbrojtur dhe variabla publike `L0N_CERT_THUMBPRINT`. Mos vendos PFX ose PIN në GitHub.
5. Dërgo vetëm **emrin e ofruesit dhe llojin e shërbimit** për konfigurim të mëtejshëm; asnjë sekret.

## Rregullat e sigurisë

- Mos e fik Smart App Control dhe mos anashkalo politikat e kompjuterit të punës.
- Asnjë `SIGNED` nuk publikohet pa verifikimin real të skedarëve.
- Certifikata nuk garanton vetvetiu leje në kompjuterët e punës.
- Mos përdor runner të hapur për PR/fork të pabesuar: një runner publik me çelës nënshkrimi është veçanërisht i ndjeshëm.

Burime: [Microsoft Smart App Control](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/code-signing-for-smart-app-control), [CA/B Forum](https://cabforum.org/working-groups/code-signing/requirements/).
