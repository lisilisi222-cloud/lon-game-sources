#!/usr/bin/env python3
"""Build a metadata-only catalog from publicly accessible WP APIs or RSS feeds.

No torrent/magnet/archive download links are collected or generated.
Does not bypass authentication, CAPTCHAs or anti-bot protections.
"""
import argparse
import html
import json
import re
import time
import urllib.error
import urllib.parse
import urllib.request
import xml.etree.ElementTree as ET
from datetime import datetime, timezone
from pathlib import Path

USER_AGENT = 'L0N-Website-Metadata-Indexer/1.0 (+personal catalog)'
MAX_RESPONSE = 3_000_000
MAX_TITLE = 200


def clean_title(v):
    v = html.unescape(re.sub(r'<[^>]+>', '', str(v)))
    return ' '.join(v.split())[:MAX_TITLE]


def is_allowed_page(url, domain):
    try:
        p = urllib.parse.urlsplit(url)
        host = (p.hostname or '').lower()
        return p.scheme == 'https' and host in (domain, 'www.' + domain) and not p.username and not p.password and p.port in (None, 443)
    except ValueError:
        return False


class SafeRedirect(urllib.request.HTTPRedirectHandler):
    def __init__(self, domain):
        self.domain = domain
        self.hops = 0
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        self.hops += 1
        if self.hops > 3 or not is_allowed_page(newurl, self.domain):
            raise ValueError('Redirect blocked: unexpected URL/domain')
        return super().redirect_request(req, fp, code, msg, headers, newurl)


def get_url(url, domain):
    if not is_allowed_page(url, domain):
        raise ValueError('Non-HTTPS or foreign domain rejected')
    opener = urllib.request.build_opener(SafeRedirect(domain))
    request = urllib.request.Request(url, headers={'User-Agent': USER_AGENT, 'Accept': 'application/json, application/rss+xml, application/atom+xml, application/xml;q=0.8'})
    with opener.open(request, timeout=16) as r:
        if r.status != 200:
            raise ValueError('HTTP ' + str(r.status))
        b = r.read(MAX_RESPONSE + 1)
        if len(b) > MAX_RESPONSE:
            raise ValueError('Response too large')
        return b


def parse_wordpress(payload, site):
    raw = json.loads(payload)
    if not isinstance(raw, list):
        raise ValueError('WordPress response must be a list')
    out = []
    for item in raw:
        if not isinstance(item, dict):
            continue
        page = item.get('link', '')
        title = clean_title((item.get('title') or {}).get('rendered', ''))
        if title and is_allowed_page(page, site['domain']):
            out.append({'title':title, 'site':site['name'], 'pageUrl':page, 'published':str(item.get('date', ''))[:10]})
    return out


def parse_feed(payload, site):
    root = ET.fromstring(payload)
    out = []
    ns = {'atom':'http://www.w3.org/2005/Atom', 'dc':'http://purl.org/dc/elements/1.1/'}
    for entry in list(root.findall('.//item')) + list(root.findall('.//atom:entry', ns)):
        title = clean_title(entry.findtext('title') or entry.findtext('atom:title', default='', namespaces=ns))
        page = entry.findtext('link') or ''
        if not page:
            for link in entry.findall('atom:link', ns):
                if link.attrib.get('rel') in (None, 'alternate'):
                    page = link.attrib.get('href', '')
                    if page:
                        break
        date = entry.findtext('pubDate') or entry.findtext('atom:published', default='', namespaces=ns) or ''
        if title and is_allowed_page(page, site['domain']):
            out.append({'title':title, 'site':site['name'], 'pageUrl':page, 'published':date[:32]})
    return out


def collect_site(site, fetch=get_url):
    domain = site['domain']
    base = 'https://' + domain
    tried = []
    # WordPress REST first, RSS fallback. No circumvention if site rejects access.
    for method, relative in [('wordpress', '/wp-json/wp/v2/posts?per_page=100&_fields=title,link,date'), ('rss', '/feed/')]:
        try:
            payload = fetch(base + relative, domain)
            parsed = parse_wordpress(payload, site) if method == 'wordpress' else parse_feed(payload, site)
            if parsed:
                return parsed, {'site':site['name'], 'method':method, 'found':len(parsed), 'status':'ok'}
            tried.append(method + ': no usable public entries')
        except (urllib.error.HTTPError, urllib.error.URLError, TimeoutError, ValueError, json.JSONDecodeError, ET.ParseError) as exc:
            tried.append(method + ': ' + str(exc)[:140])
    return [], {'site':site['name'], 'found':0, 'status':'unavailable', 'details':'; '.join(tried)}


def generate(config, fetch=get_url):
    seen = set()
    items, statuses = [], []
    for site in config['sites']:
        data, status = collect_site(site, fetch)
        statuses.append(status)
        for game in data:
            key = game['pageUrl'].lower().rstrip('/')
            if key not in seen:
                seen.add(key)
                items.append(game)
    return {'name':'L0N Website Catalog – Metadata Only',
            'generatedAt':datetime.now(timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ'),
            'kind':'website_metadata',
            'items':items,
            'note':'Website posts only. Not a download source; no automatic installers, cracks or magnets included.'}, statuses


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--config', default='sites_config.json')
    ap.add_argument('--output', default='website_catalog.json')
    ap.add_argument('--status', default='website_status.json')
    args = ap.parse_args()
    cfg = json.loads(Path(args.config).read_text(encoding='utf-8'))
    catalog, status = generate(cfg)
    Path(args.output).write_text(json.dumps(catalog, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    Path(args.status).write_text(json.dumps(status, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    for st in status:
        print(st)
    print(f'Wrote {len(catalog["items"])} public metadata entries to {args.output}')


if __name__ == '__main__':
    main()
