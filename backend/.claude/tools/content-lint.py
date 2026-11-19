#!/usr/bin/env python3
"""Mechanical quality checks for topic files (ADR 004, checklist 4), one command for authors and testers.

`ContentTool validate` checks the format (ADR 002). This script checks what an LLM used to self-check by hand,
so authors and testers can spend their effort on facts instead.

Usage (from Prepround/backend):
  python3 .claude/tools/content-lint.py <topic-id | path.yaml> [...] [--urls]
  python3 .claude/tools/content-lint.py --all [--urls]

Errors (exit 1):
  balance   every level of the field's scale (field.yaml) has the same number of questions;
  multiple  25-40 % of questions are `multiple`;
  code      at least 25 % of questions have a `code` block in language topics (LANGUAGE_TOPICS);
  lengths   the correct option is not systematically the longest, and not systematically "never the longest":
            single  - strictly longest in at most MAX_LONGEST_SHARE and at least MIN_LONGEST_SHARE of questions,
                      and where it is longest, at most MAX_RATIO times the longest distractor;
            multiple - average correct / average wrong length within 1/MAX_RATIO..MAX_RATIO;
  options   no "all of the above" / "none of the above" style options;
  sources   every question has `sources` (except the 36 seed questions, LEGACY_NO_SOURCES);
  urls      (--urls only) every source URL answers HTTP 2xx after redirects, checked with curl like authors do.
Warnings (do not fail):
  similar   stem + code + options overlap a lot with another question anywhere in the content (possible duplicate).
Exit 0 - no errors, 1 - errors, 2 - bad arguments.
"""
import argparse
import math
import re
import subprocess
import sys
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

import yaml

MAX_LONGEST_SHARE = 0.56   # at most 5 of 9 single questions
MIN_LONGEST_SHARE = 0.11   # at least 1 of 9: "never the longest" is a hint too
MAX_RATIO = 1.2
MULTIPLE_SHARE = (0.25, 0.40)
CODE_SHARE = 0.25
SIMILAR_JACCARD = 0.6

# ADR 004, 4.4: "code block in at least 25 % of language topics (csharp, sql, javascript...)".
LANGUAGE_TOPICS = {
    "csharp", "dotnet-async", "sql", "javascript", "typescript", "python-core", "python-async",
    "java-core", "java-concurrency", "go-lang", "go-concurrency", "php-lang",
}
# Seed questions were written before `sources` existed (ADR 004, 3): topic -> highest legacy number.
LEGACY_NO_SOURCES = {"csharp": 12, "ef-core": 12, "sql": 12}

FORBIDDEN_OPTION = re.compile(r"перелічен|варіант\w* вище|all of the above|none of the above", re.IGNORECASE)
WORD = re.compile(r"[\w']{3,}", re.UNICODE)

CONTENT_ROOT = Path(__file__).resolve().parents[3] / "content"


def load(path: Path) -> dict:
    return yaml.safe_load(path.read_text(encoding="utf-8")) or {}


def option_lengths(q: dict) -> tuple[list[int], list[int]]:
    opts = q.get("options") or []
    right = [len(str(o["text"]).strip()) for o in opts if o.get("correct")]
    wrong = [len(str(o["text"]).strip()) for o in opts if not o.get("correct")]
    return right, wrong


def words(text: str) -> set[str]:
    return set(WORD.findall(text.lower().replace("`", " ")))


def signature(q: dict) -> set[str]:
    """Words of the stem, code and all options: generic stems like "What does this code print?" alone match everything."""
    parts = [str(q.get("text", "")), str((q.get("code") or {}).get("source", ""))]
    parts += [str(o.get("text", "")) for o in q.get("options") or []]
    return words(" ".join(parts))


def check_url(url: str) -> tuple[str, str]:
    try:
        out = subprocess.run(
            ["curl", "-sL", "-o", "/dev/null", "-w", "%{http_code}", "-A", "Mozilla/5.0", "--max-time", "20", url],
            capture_output=True, text=True, timeout=30,
        ).stdout.strip()
    except subprocess.TimeoutExpired:
        out = "timeout"
    return url, out or "000"


def lint_topic(path: Path, levels: list[str], all_questions: list[tuple[str, str, set[str]]], urls: dict) -> tuple[list[str], list[str], str]:
    data = load(path)
    topic = data.get("id") or path.stem
    qs = data.get("questions") or []
    errors, warnings = [], []
    if not qs:
        return errors, warnings, f"{topic}: порожня тема"

    n = len(qs)
    per_level = {lv: 0 for lv in levels}
    for q in qs:
        per_level[q.get("level")] = per_level.get(q.get("level"), 0) + 1
    if len(set(per_level.values())) > 1:
        shown = " / ".join(f"{lv} {c}" for lv, c in per_level.items())
        errors.append(f"рівні нерівні: {shown}")

    multiple = sum(q.get("type") == "multiple" for q in qs)
    lo, hi = MULTIPLE_SHARE
    if not lo <= multiple / n <= hi:
        errors.append(f"multiple {multiple} з {n} ({multiple / n:.0%}), норма {lo:.0%}–{hi:.0%}")

    code = sum("code" in q for q in qs)
    if topic in LANGUAGE_TOPICS and code / n < CODE_SHARE:
        errors.append(f"code {code} з {n} ({code / n:.0%}), у мовній темі щонайменше {CODE_SHARE:.0%}")

    singles = longest = 0
    for q in qs:
        right, wrong = option_lengths(q)
        if not right or not wrong:
            continue
        if q.get("type") == "single":
            singles += 1
            if right[0] > max(wrong):
                longest += 1
                ratio = right[0] / max(wrong)
                if ratio > MAX_RATIO:
                    errors.append(f"{q['id']}: правильна {right[0]} / найдовший дистрактор {max(wrong)} ({ratio:.2f}), межа {MAX_RATIO}")
        else:
            ratio = (sum(right) / len(right)) / (sum(wrong) / len(wrong))
            if ratio > MAX_RATIO or ratio < 1 / MAX_RATIO:
                errors.append(f"{q['id']}: multiple, середня правильних / хибних {ratio:.2f} (норма {1 / MAX_RATIO:.2f}–{MAX_RATIO:.2f})")
    if singles:
        if longest / singles > MAX_LONGEST_SHARE:
            errors.append(f"правильна найдовша в {longest} з {singles} single (межа {MAX_LONGEST_SHARE:.0%})")
        need = math.ceil(singles * MIN_LONGEST_SHARE)
        if longest < need:
            errors.append(f"правильна найдовша лише в {longest} з {singles} single (треба ≥ {need}: «ніколи не найдовша» — теж підказка)")

    for q in qs:
        for o in q.get("options") or []:
            if FORBIDDEN_OPTION.search(str(o.get("text", ""))):
                errors.append(f"{q['id']}: варіант на кшталт «усе з переліченого»: {str(o['text'])[:60]}")

    for q in qs:
        m = re.search(r"-(\d+)$", str(q.get("id", "")))
        legacy = topic in LEGACY_NO_SOURCES and m and int(m.group(1)) <= LEGACY_NO_SOURCES[topic]
        if not q.get("sources") and not legacy:
            errors.append(f"{q['id']}: немає sources")
        for u in q.get("sources") or []:
            code_ = urls.get(u)
            if code_ is not None and not code_.startswith("2"):
                errors.append(f"{q['id']}: {u} → HTTP {code_}")

    for q in qs:
        mine = signature(q)
        if len(mine) < 8:
            continue
        for other_id, other_topic, other in all_questions:
            if other_id == q["id"] or not other:
                continue
            j = len(mine & other) / len(mine | other)
            if j >= SIMILAR_JACCARD:
                warnings.append(f"{q['id']}: схоже на {other_id} ({other_topic}), збіг слів {j:.0%}")

    summary = (f"{n} питань ({' / '.join(f'{lv} {c}' for lv, c in per_level.items())}) · "
               f"multiple {multiple} ({multiple / n:.0%}) · code {code} ({code / n:.0%}) · найдовша {longest}/{singles} single")
    return errors, warnings, f"{topic}: {summary}"


def resolve(arg: str) -> Path | None:
    p = Path(arg)
    if p.suffix in (".yaml", ".yml") and p.is_file():
        return p.resolve()
    hits = list(CONTENT_ROOT.glob(f"*/topics/{arg}.yaml"))
    return hits[0] if len(hits) == 1 else None


def main() -> int:
    ap = argparse.ArgumentParser(description="Mechanical quality checks for Prepround topics (ADR 004, 4).")
    ap.add_argument("topics", nargs="*", help="topic ids or paths to topic YAML files")
    ap.add_argument("--all", action="store_true", help="lint every non-empty topic")
    ap.add_argument("--urls", action="store_true", help="also check that every source URL answers HTTP 2xx")
    args = ap.parse_args()

    if args.all:
        paths = sorted(p for p in CONTENT_ROOT.glob("*/topics/*.yaml") if load(p).get("questions"))
    else:
        paths = []
        for a in args.topics:
            p = resolve(a)
            if p is None:
                print(f"не знайдено тему: {a}", file=sys.stderr)
                return 2
            paths.append(p)
    if not paths:
        ap.print_usage(sys.stderr)
        return 2

    all_questions = []
    for p in CONTENT_ROOT.glob("*/topics/*.yaml"):
        try:
            d = load(p)
        except yaml.YAMLError:
            # Another author may be mid-edit; other files only feed the duplicate check, so skip instead of failing.
            if p not in paths:
                print(f"! {p.name}: не парситься (мабуть, його саме редагують), пропущено в пошуку дублів", file=sys.stderr)
                continue
            raise
        for q in d.get("questions") or []:
            all_questions.append((q.get("id"), d.get("id"), signature(q)))

    urls: dict[str, str] = {}
    if args.urls:
        todo = sorted({u for p in paths for q in load(p).get("questions") or [] for u in q.get("sources") or []})
        with ThreadPoolExecutor(max_workers=12) as pool:
            urls = dict(pool.map(check_url, todo))

    failed = False
    for p in paths:
        field = load(p.parent.parent / "field.yaml")
        levels = [lv["id"] for lv in field.get("levels") or []]
        errors, warnings, summary = lint_topic(p, levels, all_questions, urls)
        print(("ПОМИЛКИ " if errors else "OK ") + summary)
        for e in errors:
            print(f"  ✗ {e}")
        for w in warnings:
            print(f"  ! {w}")
        failed = failed or bool(errors)
    if args.urls:
        print(f"URL перевірено: {len(urls)}")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
