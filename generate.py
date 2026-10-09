#!/usr/bin/env python3
"""L0N automatic source of legitimately released Windows game downloads.

Uses official GitHub Releases API. No scraping of repack sites, no execution,
no invented links. Discovered projects are NOT independently security audited.
"""
import argparse
import json
import os
import re
import sys
import time
from datetime import datetime, timezone
from pathlib import Path
from urllib.error import HTTPError, URLError
from urllib.parse import quote, urlencode, urlsplit
from urllib.request import Request, urlopen

API = "https://api.github.com"
REPO_RX = re.compile(r"^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$")
VALID_EXT = (".zip", ".7z", ".exe", ".msi")
WINDOWS_WORDS = re.compile(r"(?:windows?|win32|win64|win-x64|win-x86|w64|x86_64-windows|amd64-windows)", re.I)
NOT_WINDOWS = re.compile(r"(?:linux|macos|darwin|osx|android|ios|freebsd|arm64|aarch64|source|symbols|debug|pdb|sdk|server-only)", re.I)
BAD_PROJECT_WORDS = {"game-engine", "launcher", "cheats", "trainer", "crack", "keygen", "toolkit", "game-assets", "game-modding"}
HTTP_TIMEOUT = 25


def api_json(path, token="", retries=2, sleeper=time.sleep):
    """Only access public GitHub API endpoints; optional Actions token boosts quotas."""
    if not path.startswith("/") or "//" in path or ".." in path:
        raise ValueError("Unsafe GitHub API path")
    headers = {
        "Accept": "application/vnd.github+json",
        "User-Agent": "L0N-Source-Builder/1.0",
        "X-GitHub-Api-Version": "2022-11-28",
    }
    if token:
        headers["Authorization"] = "Bearer " + token
    for attempt in range(retries + 1):
        try:
            with urlopen(Request(API + path, headers=headers), timeout=HTTP_TIMEOUT) as response:
                if response.status != 200:
                    raise RuntimeError(f"GitHub API returned {response.status}")
                raw = response.read(6_000_001)
                if len(raw) > 6_000_000:
                    raise ValueError("GitHub response exceeds size limit")
                return json.loads(raw)
        except HTTPError as exc:
            if exc.code in (403, 429, 502, 503) and attempt < retries:
                sleeper(2 * (attempt + 1))
                continue
            raise RuntimeError(f"GitHub API HTTP {exc.code} for {path}") from exc
        except (URLError, TimeoutError) as exc:
            if attempt < retries:
                sleeper(2 * (attempt + 1))
                continue
            raise RuntimeError(f"GitHub API network error: {exc}") from exc


def is_windows_game_asset(asset):
    name = str(asset.get("name") or "")
    low = name.lower()
    if not low.endswith(VALID_EXT):
        return False
    if NOT_WINDOWS.search(low):
        return False
    if not WINDOWS_WORDS.search(low):
        return False
    return asset.get("size", 0) > 100_000 and asset.get("state", "uploaded") == "uploaded"


def safe_release_url(raw, repo):
    """Only URLs from this project's official GitHub release asset namespace."""
    if not isinstance(raw, str) or len(raw) > 2048:
        return False
    p = urlsplit(raw)
    if p.scheme != "https" or p.hostname != "github.com" or p.username or p.password or p.port or p.fragment:
        return False
    return p.path.startswith(f"/{repo}/releases/download/")


def size_human(n):
    try:
        n = int(n)
    except (TypeError, ValueError):
        return "Unknown"
    if n < 1_000_000:
        return f"{n / 1000:.0f} KB"
    if n < 1_000_000_000:
        return f"{n / 1_000_000:.1f} MB"
    return f"{n / 1_000_000_000:.2f} GB"


def asset_rank(asset):
    n = asset["name"].lower()
    score = 0
    if "win64" in n or "win-x64" in n or "windows-x64" in n or "x86_64-windows" in n:
        score += 30
    if "portable" in n:
        score += 14
    if n.endswith(".zip"):
        score += 8
    if n.endswith(".msi"):
        score += 6
    if "setup" in n or "installer" in n:
        score += 4
    if "win32" in n or "x86" in n:
        score -= 10
    return score


def to_game(repo, title, release):
    if not isinstance(release, dict) or release.get("draft") or release.get("prerelease"):
        return None
    candidates = [a for a in release.get("assets", []) if is_windows_game_asset(a)
                  and safe_release_url(a.get("browser_download_url"), repo)]
    if not candidates:
        return None
    asset = max(candidates, key=lambda a: (asset_rank(a), int(a.get("size", 0))))
    return {
        "title": str(title).strip()[:140],
        "fileSize": size_human(asset.get("size")),
        "uploadDate": str(release.get("published_at") or "")[:10],
        "uris": [asset["browser_download_url"]]
    }


def get_latest(repo, fetch):
    repo_path = "/repos/" + "/".join(quote(x, safe="") for x in repo.split("/"))
    try:
        return fetch(repo_path + "/releases/latest")
    except RuntimeError as exc:
        if "HTTP 404" not in str(exc):
            raise
    # Some projects only have prereleases; do not add those by default.
    releases = fetch(repo_path + "/releases?per_page=8")
    return next((r for r in releases if not r.get("draft") and not r.get("prerelease")), None)


def discover(config, fetch):
    conf = config.get("auto_discovery") or {}
    if not conf.get("enabled"):
        return []
    limit = min(100, max(0, int(conf.get("max_repositories", 30))))
    min_stars = max(0, int(conf.get("min_stars", 200)))
    found = []
    seen = set()
    for query in conf.get("queries", [])[:5]:
        if len(found) >= limit:
            break
        args = urlencode({"q": str(query), "per_page": min(100, limit), "sort": "stars", "order": "desc"})
        try:
            result = fetch("/search/repositories?" + args)
        except (RuntimeError, ValueError) as exc:
            print(f"Warning: GitHub discovery unavailable: {exc}", file=sys.stderr)
            continue
        for item in result.get("items", []):
            repo = str(item.get("full_name") or "")
            if not REPO_RX.fullmatch(repo) or repo.casefold() in seen:
                continue
            if item.get("archived") or item.get("fork") or item.get("stargazers_count", 0) < min_stars:
                continue
            topics = {str(x).lower() for x in item.get("topics", [])}
            if not topics.intersection({"game", "games", "open-source-game", "opensource-game"}):
                continue
            if topics.intersection(BAD_PROJECT_WORDS):
                continue
            desc = str(item.get("description") or "").lower()
            if any(x in desc for x in ("game engine", "game launcher", "trainer", "cheat software", "toolkit")):
                continue
            seen.add(repo.casefold())
            found.append({"repo": repo, "name": str(item.get("name") or repo.rsplit("/", 1)[-1])})
            if len(found) >= limit:
                break
    return found


def build(config, fetch):
    repos = []
    seen = set()
    for item in list(config.get("verified_project_candidates", [])) + discover(config, fetch):
        repo = str(item.get("repo", ""))
        if not REPO_RX.fullmatch(repo) or repo.lower() in seen:
            continue
        seen.add(repo.lower())
        repos.append(item)
    games = []
    for item in repos:
        repo = item["repo"]
        try:
            release = get_latest(repo, fetch)
            game = to_game(repo, item.get("name", repo.split("/")[-1]), release)
        except (RuntimeError, ValueError, KeyError) as exc:
            print(f"Skipping {repo}: {exc}", file=sys.stderr)
            continue
        if game:
            games.append(game)
            print(f"Added: {game['title']} ({game['fileSize']})")
        else:
            print(f"Skipping {repo}: no verified Windows release asset")
    games.sort(key=lambda g: g["title"].casefold())
    return {"name": config.get("source_name", "L0N Games"), "downloads": games}


def main(argv=None):
    parser = argparse.ArgumentParser(description="Automatically generate L0N/Hydra-format game source")
    parser.add_argument("--config", default="config.json")
    parser.add_argument("--output", default="games.json")
    parser.add_argument("--no-discovery", action="store_true")
    args = parser.parse_args(argv)
    config = json.loads(Path(args.config).read_text(encoding="utf-8"))
    if args.no_discovery:
        config.setdefault("auto_discovery", {})["enabled"] = False
    token = os.getenv("GITHUB_TOKEN", "")
    catalog = build(config, lambda p: api_json(p, token))
    if not catalog["downloads"]:
        raise SystemExit("No usable Windows game releases found. Previous games.json left unchanged.")
    out = Path(args.output)
    out.parent.mkdir(parents=True, exist_ok=True)
    content = json.dumps(catalog, ensure_ascii=False, indent=2) + "\n"
    temp = out.with_suffix(out.suffix + ".tmp")
    temp.write_text(content, encoding="utf-8")
    temp.replace(out)
    print(f"Saved {len(catalog['downloads'])} games to {out}")


if __name__ == "__main__":
    main()
