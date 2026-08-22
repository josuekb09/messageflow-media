"""Create a consistent SQLite snapshot with VACUUM INTO (falls back to backup)."""
from __future__ import annotations

import os
import sqlite3
import sys


def main() -> int:
    if len(sys.argv) != 3:
        print("usage: snapshot-sqlite.py <source.db> <dest.db>", file=sys.stderr)
        return 2

    src, dest = sys.argv[1], sys.argv[2]
    if os.path.exists(dest):
        os.remove(dest)

    con = sqlite3.connect(src)
    try:
        con.execute("PRAGMA wal_checkpoint(TRUNCATE)")
        dest_sql = dest.replace("'", "''")
        try:
            con.execute("VACUUM INTO '%s'" % dest_sql)
            print("vacuum into ok")
            return 0
        except sqlite3.Error as exc:
            print("VACUUM INTO failed (%s); using backup API" % exc)
            dest_con = sqlite3.connect(dest)
            try:
                con.backup(dest_con)
            finally:
                dest_con.close()
            print("backup ok")
            return 0
    finally:
        con.close()


if __name__ == "__main__":
    raise SystemExit(main())
