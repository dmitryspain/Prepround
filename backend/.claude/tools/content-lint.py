#!/usr/bin/env python3
"""Mechanical quality checks for topic files (ADR 004, checklist 4), one command for authors and testers.

`ContentTool validate` checks the format (ADR 002). This script checks what an LLM used to self-check by hand,
so authors and testers can spend their effort on facts instead.

Usage (from Prepround/backend):
  python3 .claude/tools/content-lint.py <topic-id | path.yaml> [...] [--urls] [--base <dir> [--translation-only]]
  python3 .claude/tools/content-lint.py --all [--urls] [--base <dir> [--translation-only]]

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
Translation into LANGUAGE (ADR 006, 5), lines tagged `[en]`; a topic without translations lints as before:
  lengths   the same rules over the `text_en` of options, for the fully translated questions only
            (the shares count the translated `single` questions);
  options   no "all of the above" style `text_en` of options;
  complete  if any question has a translation, every question has a full one (`text_en`, `explanation_en`,
            `text_en` of every option); the summary line then ends with ` · en N/M`;
  cyrillic  no Cyrillic outside backticks in `text_en`, `explanation_en`, `text_en` of options, `title_en`,
            `description_en`;
  code      the inline code (`...`) of an option's `text_en` is the same multiset as in its `text`.
--base <dir> compares every topic with <dir>/<field>/topics/<topic>.yaml (a copy of the content root taken
before the edit; a topic missing there is skipped with a warning). With --all it also lints the empty topics
and lists the topics that exist only in the base (a warning):
  stale     a base text (`text`, `explanation`, `text` of an option, `title`, `description`) changed while its
            `_en`, present in the base, did not;
  --translation-only: anything but `*_en` keys (and `draft_en`) differs from the base - new, missing or
            reordered questions and options, `level`, `type`, `correct`, `code`, `sources`, base texts, `draft`;
            a topic missing in the base or (with --all) only in the base is an error too.
--all skips the empty topics unless --base is given or the topic has `*_en` keys (such as `title_en`).
Warnings (do not fail):
  similar   stem + code + options overlap a lot with another question anywhere in the content (possible duplicate);
  code      [en] the set of inline code fragments in `text_en` / `explanation_en` differs from `text` / `explanation`.
Exit 0 - no errors, 1 - errors, 2 - bad arguments (also `--base` without an existing directory or without
any <field>/topics/<topic>.yaml in it, `--translation-only` without `--base`).
"""
import argparse
import math
import re
import subprocess
import sys
from collections import Counter
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
# Same pattern as the frontend's content-text pipe, so the lint sees exactly the fragments a user sees as code.
CODE_SPAN = re.compile(r"`[^`\n]+`")
CYRILLIC = re.compile(r"[\u0400-\u04FF][\u0400-\u04FF'\u2019-]*")

# ADR 006: mirrors ContentLanguages (Base, Translations) in Prepround.Application; the lint knows one translation.
BASE_LANGUAGE = "uk"
LANGUAGE = "en"
SUFFIX = f"_{LANGUAGE}"
TAG = f"[{LANGUAGE}] "

CONTENT_ROOT = Path(__file__).resolve().parents[3] / "content"


def load(path: Path) -> dict:
    return yaml.safe_load(path.read_text(encoding="utf-8")) or {}


def tr(key: str) -> str:
    """The YAML key of `key` in the translation language, such as `text_en`."""
    return key + SUFFIX


def option_lengths(q: dict, key: str = "text") -> tuple[list[int], list[int]]:
    opts = q.get("options") or []
    right = [len(str(o[key]).strip()) for o in opts if o.get("correct")]
    wrong = [len(str(o[key]).strip()) for o in opts if not o.get("correct")]
    return right, wrong


def check_lengths(qs: list[dict], key: str = "text", tag: str = "") -> tuple[list[str], int, int]:
    """The `lengths` rules over the option texts under `key`; returns errors, longest and singles."""
    errors = []
    singles = longest = 0
    for q in qs:
        right, wrong = option_lengths(q, key)
        if not right or not wrong:
            continue
        if q.get("type") == "single":
            singles += 1
            if right[0] > max(wrong):
                longest += 1
                ratio = right[0] / max(wrong)
                if ratio > MAX_RATIO:
                    errors.append(f"{tag}{q['id']}: правильна {right[0]} / найдовший дистрактор {max(wrong)} ({ratio:.2f}), межа {MAX_RATIO}")
        else:
            ratio = (sum(right) / len(right)) / (sum(wrong) / len(wrong))
            if ratio > MAX_RATIO or ratio < 1 / MAX_RATIO:
                errors.append(f"{tag}{q['id']}: multiple, середня правильних / хибних {ratio:.2f} (норма {1 / MAX_RATIO:.2f}–{MAX_RATIO:.2f})")
    if singles:
        if longest / singles > MAX_LONGEST_SHARE:
            errors.append(f"{tag}правильна найдовша в {longest} з {singles} single (межа {MAX_LONGEST_SHARE:.0%})")
        need = math.ceil(singles * MIN_LONGEST_SHARE)
        if longest < need:
            errors.append(f"{tag}правильна найдовша лише в {longest} з {singles} single (треба ≥ {need}: «ніколи не найдовша» — теж підказка)")
    return errors, longest, singles


def has_translation(q: dict) -> bool:
    return any(k.endswith(SUFFIX) for k in q) or any(k.endswith(SUFFIX) for o in q.get("options") or [] for k in o)


def translated(q: dict) -> bool:
    """Rule 11 (PRP-025): a question's translation is the text, the explanation and the text of every option."""
    opts = q.get("options") or []
    return bool(q.get(tr("text")) and q.get(tr("explanation")) and opts and all(o.get(tr("text")) for o in opts))


def cyrillic(text) -> str | None:
    """The first Cyrillic word outside inline code: code and program output are shared by both languages."""
    m = CYRILLIC.search(CODE_SPAN.sub(" ", str(text)))
    return m.group(0) if m else None


def code_diff(base: Counter, translation: Counter) -> str:
    missing = " ".join((base - translation).elements())
    extra = " ".join((translation - base).elements())
    return "; ".join(part for part in (missing and f"немає {missing}", extra and f"зайве {extra}") if part)


def lint_translation(topic: str, data: dict, qs: list[dict]) -> tuple[list[str], list[str], str]:
    """Checks of the translation (ADR 006, 5); a topic whose questions have no `_en` keys gets no summary suffix."""
    errors, warnings = [], []
    for key in ("title", "description"):
        if data.get(tr(key)) is not None and (word := cyrillic(data[tr(key)])):
            errors.append(f"{TAG}{topic}: кирилиця в {tr(key)}: {word}")

    for q in qs:
        for key in ("text", "explanation"):
            if q.get(tr(key)) is None:
                continue
            if word := cyrillic(q[tr(key)]):
                errors.append(f"{TAG}{q['id']}: кирилиця в {tr(key)}: {word}")
            base, translation = set(CODE_SPAN.findall(str(q.get(key, "")))), set(CODE_SPAN.findall(str(q[tr(key)])))
            if base != translation:
                warnings.append(f"{TAG}{q['id']}: inline-код у {tr(key)} не збігається з {key}: "
                                f"{code_diff(Counter(base), Counter(translation))}")
        for i, o in enumerate(q.get("options") or [], 1):
            if o.get(tr("text")) is None:
                continue
            text = str(o[tr("text")])
            if FORBIDDEN_OPTION.search(text):
                errors.append(f"{TAG}{q['id']}: варіант на кшталт «усе з переліченого»: {text[:60]}")
            if word := cyrillic(text):
                errors.append(f"{TAG}{q['id']}: кирилиця в {tr('text')} варіанта {i}: {word}")
            base, translation = Counter(CODE_SPAN.findall(str(o.get("text", "")))), Counter(CODE_SPAN.findall(text))
            if base != translation:
                errors.append(f"{TAG}{q['id']}: inline-код у {tr('text')} варіанта {i} не збігається з text: "
                              f"{code_diff(base, translation)}")

    done = [q for q in qs if translated(q)]
    length_errors, _, _ = check_lengths(done, tr("text"), TAG)
    errors += length_errors

    if not any(has_translation(q) for q in qs):
        return errors, warnings, ""
    if len(done) < len(qs):
        missing = ", ".join(str(q.get("id")) for q in qs if not translated(q))
        errors.append(f"{TAG}переклад неповний: {len(done)} з {len(qs)} питань (без повного перекладу: {missing})")
    return errors, warnings, f" · {LANGUAGE} {len(done)}/{len(qs)}"


def without_translation(d: dict, *drop: str) -> dict:
    """`d` without the translation keys (and `drop`): what a translation wave must leave as it was."""
    return {k: v for k, v in d.items() if not k.endswith(SUFFIX) and k not in drop}


def changed_keys(base: dict, cur: dict, prefix: str = "") -> list[str]:
    """Keys whose values differ; a nested mapping (`code`) is compared key by key, so the line names `code.source`."""
    out = []
    for k in [*base, *(k for k in cur if k not in base)]:
        b, c = base.get(k), cur.get(k)
        if isinstance(b, dict) and isinstance(c, dict):
            out += changed_keys(b, c, f"{prefix}{k}.")
        elif b != c or (k in base) != (k in cur):
            out.append(f"{prefix}{k}")
    return out


def stale_translation(topic: str, base: dict, cur: dict) -> list[str]:
    """A base text edited while its translation, present in the base, stayed as it was."""
    errors = []

    def check(id_, b: dict, c: dict, key: str, field: str):
        if b.get(tr(key)) and b.get(key) != c.get(key) and b.get(tr(key)) == c.get(tr(key)):
            errors.append(f"{id_}: змінено {BASE_LANGUAGE} без {LANGUAGE} ({field})")

    for key in ("title", "description"):
        check(topic, base, cur, key, key)
    base_qs = {q.get("id"): q for q in base.get("questions") or []}
    for q in cur.get("questions") or []:
        bq = base_qs.get(q.get("id"))
        if bq is None:
            continue
        for key in ("text", "explanation"):
            check(q["id"], bq, q, key, key)
        for i, (bo, o) in enumerate(zip(bq.get("options") or [], q.get("options") or []), 1):
            check(q["id"], bo, o, "text", f"text варіанта {i}")
    return errors


def translation_only(topic: str, base: dict, cur: dict) -> list[str]:
    """Anything but the translation keys differs from the base: a translation wave only adds `*_en` lines."""
    errors = []

    def err(id_, field: str):
        errors.append(f"{id_}: змінено не лише переклад ({field})")

    for field in changed_keys(without_translation(base, "questions"), without_translation(cur, "questions")):
        err(topic, field)

    base_ids = [q.get("id") for q in base.get("questions") or []]
    cur_ids = [q.get("id") for q in cur.get("questions") or []]
    for id_ in cur_ids:
        if id_ not in base_ids:
            err(id_, "нове питання")
    for id_ in base_ids:
        if id_ not in cur_ids:
            err(id_, "питання зникло")
    kept_base = [i for i in base_ids if i in cur_ids]
    kept_cur = [i for i in cur_ids if i in base_ids]
    for b, c in zip(kept_base, kept_cur):
        if b != c:
            err(c, "порядок питань")

    base_qs = {q.get("id"): q for q in base.get("questions") or []}
    for q in cur.get("questions") or []:
        bq = base_qs.get(q.get("id"))
        if bq is None:
            continue
        for field in changed_keys(without_translation(bq, "options"), without_translation(q, "options")):
            err(q["id"], field)
        bos = [without_translation(o) for o in bq.get("options") or []]
        cos = [without_translation(o) for o in q.get("options") or []]
        if bos == cos:
            continue
        if sorted(repr(sorted(o.items())) for o in bos) == sorted(repr(sorted(o.items())) for o in cos):
            err(q["id"], "порядок варіантів")
            continue
        for i in range(max(len(bos), len(cos))):
            if i >= len(bos):
                err(q["id"], f"варіант {i + 1} додано")
            elif i >= len(cos):
                err(q["id"], f"варіант {i + 1} зник")
            else:
                for field in changed_keys(bos[i], cos[i]):
                    err(q["id"], f"{field} варіанта {i + 1}")
    return errors


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


def lint_base(path: Path, topic: str, data: dict, base_root: Path | None, only_translation: bool) -> tuple[list[str], list[str]]:
    """Compares the topic with the same `<field>/topics/<topic>.yaml` under the base copy of the content root."""
    if base_root is None:
        return [], []
    rel = path.relative_to(path.parents[2])
    base_path = base_root / rel
    if not base_path.is_file():
        if only_translation:
            return [f"{topic}: змінено не лише переклад (нова тема: у базі немає {rel})"], []
        return [], [f"{topic}: немає в базі ({base_path}), порівняння з базою пропущено"]
    base = load(base_path)
    errors = stale_translation(topic, base, data)
    if only_translation:
        errors += translation_only(topic, base, data)
    return errors, []


def lint_topic(path: Path, levels: list[str], all_questions: list[tuple[str, str, set[str]]], urls: dict,
               base_root: Path | None = None, only_translation: bool = False) -> tuple[list[str], list[str], str]:
    data = load(path)
    topic = data.get("id") or path.stem
    qs = data.get("questions") or []
    # Both are empty for a topic without translations and without --base, so such a topic lints as before.
    en_errors, en_warnings, en_summary = lint_translation(topic, data, qs)
    base_errors, base_warnings = lint_base(path, topic, data, base_root, only_translation)
    errors, warnings = [], []
    if not qs:
        return en_errors + base_errors, en_warnings + base_warnings, f"{topic}: порожня тема"

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

    length_errors, longest, singles = check_lengths(qs)
    errors += length_errors

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

    errors += en_errors + base_errors

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
    warnings += en_warnings + base_warnings

    summary = (f"{n} питань ({' / '.join(f'{lv} {c}' for lv, c in per_level.items())}) · "
               f"multiple {multiple} ({multiple / n:.0%}) · code {code} ({code / n:.0%}) · найдовша {longest}/{singles} single")
    return errors, warnings, f"{topic}: {summary}{en_summary}"


def resolve(arg: str) -> Path | None:
    p = Path(arg)
    if p.suffix in (".yaml", ".yml") and p.is_file():
        return p.resolve()
    hits = list(CONTENT_ROOT.glob(f"*/topics/{arg}.yaml"))
    return hits[0] if len(hits) == 1 else None


def main() -> int:
    ap = argparse.ArgumentParser(description="Mechanical quality checks for Prepround topics (ADR 004, 4).")
    ap.add_argument("topics", nargs="*", help="topic ids or paths to topic YAML files")
    ap.add_argument("--all", action="store_true",
                    help="lint every non-empty topic (with --base or `*_en` keys, empty ones too)")
    ap.add_argument("--urls", action="store_true", help="also check that every source URL answers HTTP 2xx")
    ap.add_argument("--base", metavar="DIR", help="copy of the content root to compare with (stale translations)")
    ap.add_argument("--translation-only", action="store_true",
                    help="with --base: fail on any difference except the translation keys")
    args = ap.parse_args()

    base_root = None
    if args.base is not None:
        base_root = Path(args.base).resolve()
        if not args.base or not base_root.is_dir():
            print(f"не знайдено теку бази: {args.base}", file=sys.stderr)
            return 2
        if not any(base_root.glob("*/topics/*.yaml")):
            # Most likely a wrong level (content/it instead of content): every comparison would silently be skipped.
            print(f"у теці бази немає тем <галузь>/topics/<тема>.yaml: {args.base}", file=sys.stderr)
            return 2
    if args.translation_only and base_root is None:
        print("--translation-only працює лише разом з --base <тека>", file=sys.stderr)
        return 2

    gone: list[Path] = []
    if args.all:
        # An empty topic still has names to check and, against a base, questions that may have vanished;
        # without --base and translations the selection stays the non-empty topics, as before.
        paths = sorted(p for p in CONTENT_ROOT.glob("*/topics/*.yaml")
                       if base_root or (d := load(p)).get("questions") or any(k.endswith(SUFFIX) for k in d))
        if base_root:
            present = {p.relative_to(CONTENT_ROOT) for p in paths}
            gone = sorted(b.relative_to(base_root) for b in base_root.glob("*/topics/*.yaml")
                          if b.relative_to(base_root) not in present)
    else:
        paths = []
        for a in args.topics:
            p = resolve(a)
            if p is None:
                print(f"не знайдено тему: {a}", file=sys.stderr)
                return 2
            paths.append(p)
    if not paths and not gone:
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
        errors, warnings, summary = lint_topic(p, levels, all_questions, urls, base_root, args.translation_only)
        print(("ПОМИЛКИ " if errors else "OK ") + summary)
        for e in errors:
            print(f"  ✗ {e}")
        for w in warnings:
            print(f"  ! {w}")
        failed = failed or bool(errors)
    for rel in gone:
        # The file is gone, so its id is the file name (ADR 002, rule 3).
        if args.translation_only:
            print(f"ПОМИЛКИ {rel.stem}: тема зникла")
            print(f"  ✗ {rel.stem}: змінено не лише переклад (тема зникла: {rel})")
            failed = True
        else:
            print(f"OK {rel.stem}: тема зникла")
            print(f"  ! {rel.stem}: є в базі, але немає в контенті ({rel})")
    if args.urls:
        print(f"URL перевірено: {len(urls)}")
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
