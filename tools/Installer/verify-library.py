"""Verify the bundled MessageFlow library counts before packaging."""
from __future__ import annotations

import sqlite3
import sys


EXPECTED_SERMONS = {"en": 1208, "fr": 384, "sw": 622}
EXPECTED_SONGS = {"en": 357, "fr": 499, "sw": 281}
EXPECTED_BIBLES = {"KJV", "LSG", "SWHULB"}


def grouped_counts(cur: sqlite3.Cursor, sql: str) -> dict[str, int]:
    return {row[0] or "": int(row[1]) for row in cur.execute(sql)}


def main() -> int:
    if len(sys.argv) != 2:
        print("usage: verify-library.py <database.db>", file=sys.stderr)
        return 2

    path = sys.argv[1]
    con = sqlite3.connect(path)
    try:
        cur = con.cursor()
        sermons = grouped_counts(cur, "SELECT Language, COUNT(*) FROM Sermons GROUP BY Language")
        songs = grouped_counts(cur, "SELECT Language, COUNT(*) FROM Songs GROUP BY Language")
        bibles = {row[0] for row in cur.execute("SELECT Abbreviation FROM BibleTranslations")}
    finally:
        con.close()

    print("sermons", sermons)
    print("songs", songs)
    print("bibles", sorted(bibles))

    errors: list[str] = []
    for lang, expected in EXPECTED_SERMONS.items():
        actual = sermons.get(lang, 0)
        if actual != expected:
            errors.append("%s sermons expected %s got %s" % (lang, expected, actual))
    for lang, expected in EXPECTED_SONGS.items():
        actual = songs.get(lang, 0)
        if actual != expected:
            errors.append("%s songs expected %s got %s" % (lang, expected, actual))
    missing_bibles = EXPECTED_BIBLES - bibles
    if missing_bibles:
        errors.append("missing bibles: " + ", ".join(sorted(missing_bibles)))

    if errors:
        print("bundled library mismatch: " + "; ".join(errors), file=sys.stderr)
        return 1

    print("library snapshot verified")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
