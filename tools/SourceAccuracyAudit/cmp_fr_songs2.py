"""French hymns, section by section: every DB section must occur verbatim (word level, typography normalised)
in the songbook PDF; every PDF word must be covered by some section (except headings / page furniture)."""
import sqlite3, re, difflib, subprocess, json, sys, collections
import os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cmp_sermons import norm, WORD

PDF = r"E:\My Projects\MessageFlow\database\custom-library\songs\Receuil-de-cantiques-francais-TABERNACLE-DINANGA-CORRIGE.pdf"
import os
DB = os.environ.get("MF_DB", r"E:\My Projects\MessageFlow\database\messageflow.db")
con = sqlite3.connect(f'file:{DB}?mode=ro', uri=True)
txt = subprocess.run(['pdftotext', '-enc', 'UTF-8', PDF, '-'], capture_output=True).stdout.decode('utf-8')
lines = [l for l in txt.split('\n') if not re.match(r'^\s*\S?\s*Page \d+ sur \d+\s*$', l)]
P = norm('\n'.join(lines))
Ps = ' ' + ' '.join(P) + ' '
# char offset -> token index
offs = []; pos = 1
for t in P:
    offs.append(pos); pos += len(t) + 1
import bisect
covered = [False] * len(P)

songs = con.execute("select Id, Title from Songs where Language='fr' and IsActive=1").fetchall()
ok = bad = 0
problems = []
for sid, title in songs:
    for order, label, t in con.execute('select SectionOrder, SectionLabel, Text from SongSections where SongId=? order by SectionOrder', (sid,)):
        tk = norm(t)
        needle = ' ' + ' '.join(tk) + ' '
        i = Ps.find(needle)
        if i >= 0:
            ok += 1
            while i >= 0:
                a = bisect.bisect_left(offs, i + 1)
                for k in range(a, a + len(tk)): covered[k] = True
                i = Ps.find(needle, i + 1)
            continue
        bad += 1
        # locate best region: anchor on first 6 tokens / last 6 tokens
        best = None
        for anchor in (tk[:6], tk[-6:], tk[len(tk)//2:len(tk)//2+6]):
            j = Ps.find(' ' + ' '.join(anchor) + ' ')
            if j >= 0:
                best = bisect.bisect_left(offs, j + 1); break
        diffs = []
        if best is not None:
            start = max(0, best - (0 if anchor is tk[:6] else len(tk)))
            region = P[start:start + 2 * len(tk) + 20]
            sm = difflib.SequenceMatcher(None, tk, region, autojunk=False)
            blocks = sm.get_matching_blocks()
            lo = blocks[0].b; hi = blocks[-2].b + blocks[-2].size if len(blocks) > 1 else lo
            sm = difflib.SequenceMatcher(None, tk, region[lo:hi], autojunk=False)
            for op, i1, i2, j1, j2 in sm.get_opcodes():
                if op != 'equal':
                    diffs.append((op, ' '.join(tk[i1:i2]), ' '.join(region[lo + j1:lo + j2])))
            for k in range(start + lo, start + hi): covered[k] = True
        problems.append({'song': title, 'section': order, 'label': label, 'diffs': diffs[:8],
                         'located': best is not None, 'text': t[:200]})

# uncovered PDF runs
runs = []; k = 0
while k < len(P):
    if not covered[k]:
        j = k
        while j < len(P) and not covered[j]: j += 1
        runs.append(' '.join(P[k:j])); k = j
    else: k += 1
print(f'sections verbatim in PDF: {ok}; not verbatim: {bad}; uncovered PDF runs: {len(runs)}')
json.dump({'problems': problems, 'uncovered': runs}, open('fr_songs2.json', 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
