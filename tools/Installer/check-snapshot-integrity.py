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
