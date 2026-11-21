"""Run PRAGMA integrity_check on the bundled snapshot and repair index-only defects.

Only indexes that integrity_check names are rebuilt, one REINDEX each. Any problem that does not
name an index (table or page damage) cannot be repaired this way, so the build fails instead.
"""
from __future__ import annotations

import re
import sqlite3
import sys
import time

# "row 57337 missing from index X", "wrong # of entries in index X", "non-unique entry in index X"
INDEX_PROBLEM = re.compile(r"\bindex (\S+)$")
# "malformed inverted index for FTS5 table main.SermonParagraphsFts"
FTS_PROBLEM = re.compile(r"malformed inverted index for FTS5 table (?:main\.)?(\S+)$")


def integrity_check(con: sqlite3.Connection) -> list[str]:
    started = time.time()
    rows = [row[0] for row in con.execute("PRAGMA integrity_check").fetchall()]
    print("integrity_check (%.0fs): %s" % (time.time() - started, "; ".join(rows)))
    return rows


def main() -> int:
    if len(sys.argv) != 2:
        print("usage: check-snapshot-integrity.py <snapshot.db>", file=sys.stderr)
        return 2

    con = sqlite3.connect(sys.argv[1])
    try:
        problems = integrity_check(con)
        if problems == ["ok"]:
            print("snapshot integrity ok")
            return 0

        # VACUUM INTO can leave an FTS5 full-text index malformed even when the source is clean
        # (seen in 1.0.11 with 537k paragraphs). The index is derived from the paragraph text, so
        # rebuilding it changes no text. Only FTS5 tables named by integrity_check are rebuilt,
        # each one is checked with its own integrity-check afterwards, and any other problem
        # still fails the build below.
        remaining: list[str] = []
        for problem in problems:
            fts = FTS_PROBLEM.search(problem)
            if fts is None:
                remaining.append(problem)
                continue
            table = fts.group(1)
            print("FTS5 rebuild %s" % table)
            quoted = '"%s"' % table.replace('"', '""')
            con.execute("INSERT INTO %s(%s) VALUES('rebuild')" % (quoted, quoted))
            con.commit()
            con.execute("INSERT INTO %s(%s) VALUES('integrity-check')" % (quoted, quoted))
        problems = remaining
        if not problems:
            if integrity_check(con) != ["ok"]:
                print("FAIL: snapshot is still not clean after the FTS5 rebuild")
                return 1
            print("snapshot integrity repaired (FTS5 rebuild) and ok")
            return 0

        index_names: list[str] = []
        for problem in problems:
            match = INDEX_PROBLEM.search(problem)
            if match is None:
                print("FAIL: not an index-only problem, cannot repair: %s" % problem)
                return 1
            if match.group(1) not in index_names:
                index_names.append(match.group(1))

        known = {row[0] for row in con.execute("SELECT name FROM sqlite_master WHERE type = 'index'")}
        for name in index_names:
            if name not in known:
                print("FAIL: integrity_check named an index that does not exist: %s" % name)
                return 1

            print("REINDEX %s" % name)
            con.execute('REINDEX "%s"' % name.replace('"', '""'))
        con.commit()

        if integrity_check(con) != ["ok"]:
            print("FAIL: snapshot is still not clean after REINDEX")
            return 1

        print("snapshot integrity repaired and ok")
        return 0
    finally:
        con.close()


if __name__ == "__main__":
    raise SystemExit(main())
