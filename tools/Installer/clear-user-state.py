"""Remove operator state from the database snapshot bundled into the installer.

The snapshot is copied from the working database, which also holds the favorites and projection
history of whoever tested on the build machine. A fresh church install must start with none of
them. Only the snapshot is changed; the working database keeps its own state.
"""
from __future__ import annotations

import sqlite3
import sys

USER_STATE_TABLES = ("FavoriteParagraphs", "BibleFavoriteVerses", "ProjectionHistories")


def main() -> int:
    if len(sys.argv) != 2:
        print("usage: clear-user-state.py <snapshot.db>", file=sys.stderr)
        return 2

    con = sqlite3.connect(sys.argv[1])
    try:
        existing = {row[0] for row in con.execute("SELECT name FROM sqlite_master WHERE type = 'table'")}
        with con:
            for table in USER_STATE_TABLES:
                if table in existing:
                    removed = con.execute(f'DELETE FROM "{table}"').rowcount
                    print(f"{table}: removed {removed} row(s)")

        leftovers = {
            table: con.execute(f'SELECT COUNT(*) FROM "{table}"').fetchone()[0]
            for table in USER_STATE_TABLES
            if table in existing
        }
        if any(leftovers.values()):
            print(f"user state still present: {leftovers}", file=sys.stderr)
            return 1

        print("user state cleared")
        return 0
    finally:
        con.close()


if __name__ == "__main__":
    raise SystemExit(main())
