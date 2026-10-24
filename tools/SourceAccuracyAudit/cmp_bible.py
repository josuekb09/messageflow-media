"""Compare every Bible verse in the database with the original source files (not the intermediate CSVs)."""
import sqlite3, re, os, glob, sys, unicodedata, collections

import os
DB = os.environ.get("MF_DB", r"E:\My Projects\MessageFlow\database\messageflow.db")
con = sqlite3.connect(f'file:{DB}?mode=ro', uri=True)
books = [r[0] for r in con.execute('select Id from BibleBooks order by BookOrder')]


def db_verses(tid):
    d = collections.OrderedDict()
    for b, c, v, t in con.execute('select BookId,Chapter,Verse,Text from BibleVerses where TranslationId=? '
                                  'order by BookId,Chapter,Verse', (tid,)):
        d[(b, c, v)] = t
    return d


def ws(s):
    return re.sub(r'\s+', ' ', unicodedata.normalize('NFC', s)).strip()


def kjv_source():
    txt = open(r'E:\Bible\Sources\kjv_gutenberg.txt', encoding='utf-8-sig').read()
    start = txt.index('1:1 In the beginning')
    end = txt.index('*** END OF THE PROJECT GUTENBERG')
    body = txt[start:end]
    parts = re.split(r'(?:(?<=\s)|^)(\d+):(\d+)\s', body)
    out = collections.OrderedDict()
    bi = -1
    for i in range(1, len(parts), 3):
        c, v, t = int(parts[i]), int(parts[i + 1]), parts[i + 2]
        if c == 1 and v == 1:
            bi += 1
        # drop book headings that sit between the last verse of a book and 1:1 of the next
        t = re.split(r'\n\s*\n\s*\n', t)[0]
        out[(books[bi], c, v)] = ws(t)
    return out


def lsg_source():
    out = collections.OrderedDict()
    files = sorted(glob.glob(r'E:\MessageFlow-Bible-Sources\LSG1910\fraLSG_*_read.txt'))
    bookcodes = []
    for f in files:
        m = re.search(r'fraLSG_(\d{3})_([A-Z0-9]{3})_(\d+)_read', f)
        if not m or m.group(2) == '000':
            continue
        if m.group(2) not in bookcodes:
            bookcodes.append(m.group(2))
        bi = bookcodes.index(m.group(2))
        ch = int(m.group(3))
        lines = [l.strip() for l in open(f, encoding='utf-8-sig').read().split('\n') if l.strip()]
        lines = lines[2:]  # book name, chapter number
        for v, l in enumerate(lines, 1):
            out[(books[bi], ch, v)] = ws(l)
    return out, bookcodes


def swa_source():
    out = collections.OrderedDict()
    files = sorted(glob.glob(r'E:\MessageFlow-Bible-Sources\SWA\*.usfm'))
    for bi, f in enumerate(files):
        s = open(f, encoding='utf-8-sig').read()
        s = re.sub(r'\\f .*?\\f\*', '', s, flags=re.S)
        s = re.sub(r'\\x .*?\\x\*', '', s, flags=re.S)
        ch = 0
        cur = None
        for line in s.split('\n'):
            m = re.match(r'\\c (\d+)', line)
            if m:
                ch = int(m.group(1)); cur = None; continue
            for piece in re.split(r'(\\v \d+[-\d]*\s)', line):
                mv = re.match(r'\\v (\d+)', piece)
                if mv:
                    cur = (books[bi], ch, int(mv.group(1))); out[cur] = ''
                    continue
                if cur is None:
                    continue
                if re.match(r'\s*\\(id|ide|h|toc\d|mt\d?|ms\d?|mr|s\d?|r|d|sp|cl|cp)\b', piece):
                    continue
                t = re.sub(r'\\[a-z]+\d?\*?', ' ', piece)
                out[cur] = (out[cur] + ' ' + t)
    return collections.OrderedDict((k, ws(v)) for k, v in out.items()), files


def report(name, db, src):
    missing = [k for k in src if k not in db]
    extra = [k for k in db if k not in src]
    diff = [k for k in src if k in db and ws(db[k]) != src[k]]
    empty = [k for k, v in db.items() if not v.strip()]
    print(f'== {name}: db={len(db)} source={len(src)} identical={len(src) - len(missing) - len(diff)} '
          f'missing={len(missing)} extra={len(extra)} different={len(diff)} empty={len(empty)}')
    for k in missing[:10]: print('  MISSING', k, src[k][:120])
    for k in extra[:10]: print('  EXTRA', k, db[k][:120])
    for k in diff[:15]:
        print('  DIFF', k); print('     db :', db[k][:300]); print('     src:', src[k][:300])
    # numbering continuity
    gaps = 0
    prev = None
    for (b, c, v) in db:
        if prev and prev[0] == b and prev[1] == c and v != prev[2] + 1:
            gaps += 1
            if gaps <= 5: print('  GAP', prev, '->', (b, c, v))
        prev = (b, c, v)
    print('  numbering gaps inside chapters:', gaps)
    return diff


which = sys.argv[1:] or ['kjv', 'lsg', 'swa']
if 'kjv' in which:
    report('KJV vs kjv_gutenberg.txt', db_verses(1), kjv_source())
if 'lsg' in which:
    s, codes = lsg_source()
    print('LSG books:', len(codes))
    report('LSG vs LSG1910 read txt', db_verses(2), s)
if 'swa' in which:
    s, files = swa_source()
    print('SWA usfm files:', len(files))
    report('SWHULB vs usfm', db_verses(3), s)
