# L0N Auto Game Sources — Falas në GitHub

Ky është gjeneruesi që krijon automatikisht `games.json` për L0N Launcher. Nuk ka nevojë të shkruash lojërat një nga një. GitHub Actions e ekzekuton çdo ditë, ose kur prek **Run workflow**.

**Kujdes:** Katalogu mbledh vetëm publikime për Windows nga projektet e lojërave në GitHub me download zyrtar. **Nuk** përfshin automatikisht lojërat komerciale 2000–2026, lojëra nga faqe repack, apo licencat e tyre. Kodet nuk ekzekutojnë dhe nuk instalojnë asnjë skedar të shkarkuar. Për shkarkimet nga projekte të zbuluara automatikisht, verifiko besueshmërinë dhe nënshkrimin e botuesit para instalimit.

## Hapi 1 — Krijo repository (jo Gist)
1. Hape https://github.com/new dhe kyçu.
2. Emri: `lon-game-sources`.
3. Zgjedh **Public** dhe kliko **Create repository**.
4. Shkarko këtë ZIP dhe bëj **Extract All** në PC.

## Hapi 2 — Ngarko skedarët e gjeneruesit
Në repository të ri përdor **Add file → Upload files** për skedarët `generate.py`, `config.json`, `README_SHQIP.md` (mund edhe `tests/`). Kliko **Commit changes**.

## Hapi 3 — Krijo GitHub Action
1. Kliko **Add file → Create new file**.
2. Te emri i skedarit shkruaj pikërisht `.github/workflows/update-games.yml`.
3. Hape skedarin me të njëjtin emër brenda këtij ZIP (me Notepad), kopjo tekstin dhe ngjite në GitHub.
4. Kliko **Commit changes**.
5. Te **Settings → Actions → General → Workflow permissions**, sigurohu që lejohet `Read and write permissions` (kur repo-ja e ofron), pastaj **Save**.

## Hapi 4 — Gjenero automatikisht
1. Shko te **Actions**.
2. Zgjidh **Update L0N Game Source**.
3. Kliko **Run workflow → Run workflow**.
4. Prit derisa puna të bëhet me shenjë të gjelbër.
5. Kthehu te **Code**. Duhet të shfaqet `games.json`.

## Hapi 5 — Lidhu me L0N Launcher
Nëse përdoruesi yt në GitHub është `EMRIYT`, linku është:

`https://raw.githubusercontent.com/EMRIYT/lon-game-sources/main/games.json`

Zëvendëso `EMRIYT` me emrin tënd të GitHub, pastaj vendose linkun te **L0N → SOURCES → LOAD GAMES**. Nëse repo-ja përdor degën `master`, zëvendëso `main` me `master`.

`games.json` krijohet nga veprimi i parë, jo nga ZIP-i. Nëse gjenerimi dështon, hap **Actions** dhe shiko logun e hapit *Build games.json*.

## Zgjerimi i katalogut
Te `config.json` ke dy lloje burimesh:
- `verified_project_candidates`: disa projekte të njohura të lojërave. Programi shkarkon metadatat e versioneve të tyre automatikisht.
- `auto_discovery`: kërkim automatik për projekte të tjera open-source me publikime për Windows. Nuk garanton që çdo projekt i zbuluar është lojë cilësore apo binar i sigurt. Ndrysho `max_repositories` deri në 100, sipas kufijve të API-së.

Kur përditësohet JSON, e **njëjta lidhje** vazhdon të përdoret. Në L0N mund të të duhet të shtypësh sërish LOAD GAMES për rifreskim nëse launcher-i nuk e rifreskon vetë.

## Për test lokal në Windows
Me Python 3 të instaluar, hap CMD te folderi dhe nis:

`python generate.py --config config.json --output games.json`

Për testet pa internet:

`python -m unittest discover -s tests -v`

## Kufizime
- Funksionon për ato lojëra që publikojnë një skedar Windows të shkarkueshëm (`.zip`, `.exe`, `.msi`, `.7z`) në GitHub Releases.
- Nëse Github API ka rate-limit ose një projekt nuk ka release Windows, ai anashkalohet. Nëse s'ka asnjë rezultat, nuk e mbishkruan JSON-in e mëparshëm.
- Skedarët e një loje nuk instalohen automatikisht. Në veçanti, një `.exe` është vetëm download; L0N nuk e ekzekuton vetë.
- GitHub Actions nuk e përditëson katalogun derisa ta aktivizosh në repository tënd. Për të kontrolluar lojëra të paguara përdor API-të zyrtare dhe licencat përkatëse, jo linke që shpërndajnë përmbajtje pa autorizim.
