"""Independent comparison: database sermon text vs. source PDF text (Poppler pdftotext).

Word-level alignment after typography normalisation (curly quotes/dashes/ellipsis -> ASCII, which is the
database convention). Page furniture (page numbers, all-caps running heads, copyright trailer) is removed
from the PDF side. Every remaining difference is reported with its paragraph number.
"""
import sqlite3, subprocess, sys, re, json, difflib, os
from concurrent.futures import ProcessPoolExecutor

DB = os.environ.get('MF_DB', r"E:\My Projects\MessageFlow\database\messageflow.db")

TYPO = str.maketrans({'\u2019': "'", '\u2018': "'", '\u201c': '"', '\u201d': '"', '\u2014': '-', '\u2013': '-',
                      '\u00a0': ' ', '\u00ad': '', '\u2026': '...', '\u00ab': '"', '\u00bb': '"', '\u2010': '-',
                      '\u2011': '-', '\u201e': '"', '\u2032': "'", '\u02bc': "'", '\ufb01': 'fi', '\ufb02': 'fl',
                      '\u202f': ' ', '\u2009': ' ', '\ufeff': ''})
WORD = re.compile(r'\w')


def norm(s):
    s = s.translate(TYPO)
    s = re.sub(r'([-"\'.,;:!?()\[\]])', r' \1 ', s)
    return s.split()


def is_furniture(line):
    t = line.strip()
    if not t:
        return True
    if re.fullmatch(r'\d{1,4}', t):
        return True
    letters = re.sub(r'[^A-Za-z\u00C0-\u024F]', '', t)
    if len(letters) >= 4 and letters.upper() == letters and len(t) < 160:
        return True
    return False


TRAILER = re.compile(r'ALL RIGHTS RESERVED|Copyright Notice|TOUS DROITS|HAKI ZOTE|Avis de droits|VOICE OF GOD RECORDINGS')


def pdf_text(path):
    r = subprocess.run(['pdftotext', '-enc', 'UTF-8', path, '-'], capture_output=True)
    lines = r.stdout.decode('utf-8', 'replace').split('\n')
    for k in range(len(lines)):
        if TRAILER.search(lines[k]) and k > len(lines) * 0.5:
            cut = k
            # drop the 1-6 metadata lines just above (code/title, place, language)
            back = 0
            while cut > 0 and back < 6 and len(lines[cut - 1]) < 120 and not re.match(r'^\d+\s+\S', lines[cut - 1]):
                cut -= 1
                back += 1
            lines = lines[:cut]
            break
    return '\n'.join(l for l in lines if not is_furniture(l))


def compare(args):
    sid, path, lang = args
    con = sqlite3.connect(f'file:{DB}?mode=ro', uri=True)
    paras = con.execute('select ParagraphNumber, Text from SermonParagraphs where SermonId=? order by ParagraphNumber, Id',
                        (sid,)).fetchall()
    con.close()
    res = {'id': sid, 'path': path, 'lang': lang, 'paras': len(paras), 'issues': []}
    nums = [p[0] for p in paras]
    if nums:
        if nums != list(range(nums[0], nums[0] + len(nums))):
            gaps = [(a, b) for a, b in zip(nums, nums[1:]) if b != a + 1]
            res['issues'].append({'kind': 'NUMBERING', 'detail': str(gaps[:10])})
    empty = [n for n, t in paras if not t.strip()]
    if empty:
        res['issues'].append({'kind': 'EMPTY', 'detail': str(empty)})
    if not os.path.exists(path):
        res['issues'].append({'kind': 'NO_SOURCE', 'detail': path})
        return res
    ptoks = norm(pdf_text(path))
    dtoks, owner = [], []
    for n, t in paras:
        tk = norm(t)
        dtoks += tk
        owner += [n] * len(tk)
    sm = difflib.SequenceMatcher(None, dtoks, ptoks, autojunk=False)
    matched = 0
    for op, i1, i2, j1, j2 in sm.get_opcodes():
        if op == 'equal':
            matched += i2 - i1
            continue
        d, p = dtoks[i1:i2], ptoks[j1:j2]
        para = owner[min(i1, len(owner) - 1)] if owner else None
        dw = [t for t in d if WORD.search(t)]
        pw = [t for t in p if WORD.search(t)]
        if op == 'insert' and pw and all(w.isdigit() for w in pw) and len(pw) <= 2:
            kind = 'PDF_NUMBER'          # paragraph number printed in the PDF
        elif not dw and not pw:
            kind = 'PUNCT'
        elif ''.join(dw) == ''.join(pw):
            kind = 'SPACING'             # same letters, different word boundaries (glued / split words)
        elif op == 'delete':
            kind = 'DB_ONLY'
        elif op == 'insert':
            kind = 'PDF_ONLY'
        else:
            kind = 'DIFF'
        res['issues'].append({'kind': kind, 'para': para, 'db': ' '.join(d)[:400], 'pdf': ' '.join(p)[:400],
                              'ctx': ' '.join(dtoks[max(0, i1 - 8):i1])[-120:]})
    res['db_words'] = len(dtoks)
    res['pdf_words'] = len(ptoks)
    res['matched'] = matched
    return res


def main():
    out, where = sys.argv[1], sys.argv[2]
    con = sqlite3.connect(f'file:{DB}?mode=ro', uri=True)
    rows = con.execute(f'select s.Id, s.SourceFilePath, s.Language from Sermons s join Authors a on a.Id=s.AuthorId '
                       f'where {where} order by s.Id').fetchall()
    con.close()
    jobs = [(sid, re.sub(r'^[Dd]:', 'E:', p), lang) for sid, p, lang in rows]
    with ProcessPoolExecutor(max_workers=int(os.environ.get('MF_WORKERS', '8'))) as ex, \
            open(out, 'w', encoding='utf-8') as f:
        for i, r in enumerate(ex.map(compare, jobs, chunksize=1)):
            f.write(json.dumps(r, ensure_ascii=False) + '\n')
            f.flush()
            if i % 50 == 0:
                print(i, len(jobs), flush=True)


if __name__ == '__main__':
    main()
