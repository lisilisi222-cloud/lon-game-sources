# L0N – katalog automatik nga 3 website (vetëm metadata)

Kjo shtesë lexon, kur janë publikisht të disponueshme, **titujt**, **datën** dhe **linkun e faqes së artikullit** nga SteamRIP, FitGirl dhe DODI. Nuk mbledh magnet, crack, instalues apo lidhje shkarkimi të lojërave. Respekton përgjigjet HTTP 403 dhe nuk përpiqet t'i anashkalojë.

## Hapat në repository `lon-game-sources`

1. Ngarko në rrënjën e repository-t `site_catalog.py` dhe `sites_config.json` përmes **Add file → Upload files → Commit changes**.
2. Hape skedarin ekzistues `.github/workflows/update-games.yml`, kliko lapsin **Edit**, dhe zëvendëso krejt përmbajtjen me YAML-in nga kjo paketë. Bëj **Commit changes**.
3. Shko te **Actions → Update L0N Game Source → Run workflow**.
4. Te **Code** kontrollo që janë krijuar `website_catalog.json` dhe `website_status.json`. Status tregon cilat site u hapën e cilat jo.

Linku i metadata-katalogut (ndërro USERNAME me emrin tënd):
`https://raw.githubusercontent.com/USERNAME/lon-game-sources/main/website_catalog.json`

`games.json` vazhdon të mbajë download-et zyrtare/open-source nga GitHub Releases. **L0N ekzistues nuk e lexon `website_catalog.json` te GAMES**, sepse ka një format tjetër dhe nuk përmban linke direkte për instalim. Për ta shfaqur këtë metadata-katalog në aplikacion duhet shtuar faqja `Browse Websites` me butonin **OPEN PAGE** (jo DOWNLOAD). Mos e vendos `pageUrl` si `uris` për shkarkim: do të shkarkonte një faqe HTML në vend të lojës.

Nëse cilido website kthen 403, Cloudflare, timeout ose nuk ka API/RSS publik, ai shënohet në `website_status.json` si `unavailable` dhe katalogu nuk mbushet për atë website. Nuk garantohen të dhëna nga këto tri faqe.

Për lojëra me shkarkime të lejuara, `games.json` vazhdon të punojë si më parë.
