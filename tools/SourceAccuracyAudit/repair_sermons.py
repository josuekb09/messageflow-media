"""Analyse every sermon against its source PDF and build a repair plan derived only from the PDF.

Automatic fixes (everything else is reported for review, never changed):
  SPACING   same characters, different spaces           (glued words "Ifit" -> "If it")
  EMDASH    the PDF has an em dash the database lost   ("aa" -> "a-a"; database writes em dashes as "-")
  HEADER    a running page head + page number glued into a paragraph, verified against the PDF's own heads
  NUMWORD   a number of the text that the old import took for a paragraph number ("from 85 back to 100")
  SEGMENT   paragraph boundaries / numbers re-cut to follow the numbers printed in the PDF
"""
import json, os, re, sys
from concurrent.futures import ProcessPoolExecutor
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sermonlib import translate, tokens, pdf_body, align, connect, source_path, WORD, TYPO

PNUM_LINE = re.compile(r'^(\d{1,4})\s+\S', re.M)
_VOCAB = None


def vocab(lang):
    global _VOCAB
    if _VOCAB is None:
        import pickle
        _VOCAB = pickle.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'vocab.pkl'), 'rb'))
    return _VOCAB.get(lang, {})


def words_known(new, old, lang, minimum=3):
    """Every word the fix creates must already be a common word of this language's library."""
    v = vocab(lang)
    oldw = {t.lower() for t, _, _ in tokens(old)}
    for t, _, _ in tokens(new):
        if WORD.search(t) and t.lower() not in oldw and v.get(t.lower(), 0) < minimum and not t.isdigit():
            return False
    return True


def caps(s):
    return re.sub(r'[^A-ZÀ-ÖØ-Þ]', '', translate(s).upper())


def translate_with_map(raw):
    out, rawpos = [], []
    for i, ch in enumerate(raw):
        rep = TYPO.get(ch, ch)
        for c in rep:
            out.append(c); rawpos.append(i)
    return ''.join(out), rawpos


def letters(s):
    return re.sub(r'\s+', '', s)


def pdf_side(path, title):
    body, furn = pdf_body(path)
    # the title printed above paragraph 1 (mixed case in the French and Kiswahili books) is not paragraph text
    lines = body.split('\n')
    k = 0
    while k < len(lines) and k < 6 and (not lines[k].strip() or (caps(lines[k]) and caps(lines[k]) in caps(title))):
        k += 1
    body = '\n'.join(lines[k:])
    # an em dash wrapped at the end of a PDF line comes out of pdftotext as "— "
    body = re.sub('—[ \t]+(?=\\w)', '—', body)
    body = body.replace('', '').replace('', '')
    text, rawpos = translate_with_map(body)
    toks = tokens(text)
    # printed paragraph numbers, read in sequence: always the next expected number, either at the start
    # of a line or right after the end of a sentence and before a capital / quote / bracket
    line_starts = {m.start() for m in PNUM_LINE.finditer(text)}
    numtok = {}
    expected = None
    for idx, (t, s, e) in enumerate(toks):
        if not t.isdigit():
            continue
        n = int(t)
        at_line = s in line_starts
        prev = toks[idx - 1][0] if idx else '.'
        nxt = toks[idx + 1][0] if idx + 1 < len(toks) else ''
        after_sentence = prev in ('.', '!', '?', '"', "'", ')', ']') and (nxt[:1].isupper() or nxt in ('"', "'", '[', '('))
        if expected is None:
            if n <= 5 and (at_line or after_sentence):
                numtok[idx] = n; expected = n + 1
        elif n == expected and (at_line or after_sentence):
            numtok[idx] = n; expected = n + 1
        elif at_line and expected < n <= expected + 2:
            numtok[idx] = n; expected = n + 1
    import collections
    count = collections.Counter(caps(f) for f in furn)
    heads = [h for h, c in count.items() if len(h) >= 6 and c >= 2]
    return text, body, rawpos, toks, numtok, heads


def db_side(paras):
    toks = []  # (token, para_index, start, end)
    for pi, (_, _, txt, _) in enumerate(paras):
        for t, s, e in tokens(txt):
            toks.append((t, pi, s, e))
    return toks


def is_header_like(dtok):
    s = ' '.join(t for t, *_ in dtok)
    if re.search(r'[a-zß-öø-ÿ]', s):
        return False
    return len(caps(s)) >= 6


def header_verified(dtok, heads, title):
    """The glued text is one of this PDF's own running heads (printed on 2+ pages), optionally with its
    page number; a running head may be cut short with "..." in the PDF."""
    s = ' '.join(t for t, *_ in dtok)
    s = re.sub(r'\s*\d{1,4}\s*$', '', s)
    l = caps(s)
    if len(l) < 6:
        return False
    return any(l == h or (l in h and len(l) >= 0.6 * len(h)) or (h in l and len(h) >= 0.8 * len(l)) for h in heads)


def analyse(args):
    sid, = args
    con = connect()
    s = con.execute('select Title, SourceFilePath, Language from Sermons where Id=?', (sid,)).fetchone()
    title, spath, lang = s
    paras = [list(r) for r in con.execute(
        'select Id, ParagraphNumber, Text, PageNumber from SermonParagraphs where SermonId=? order by ParagraphNumber, Id',
        (sid,))]
    con.close()
    res = {'id': sid, 'title': title, 'lang': lang, 'paras': len(paras), 'fixes': [], 'review': [], 'segment': None}
    path = source_path(spath)
    if not os.path.exists(path):
        res['review'].append({'kind': 'NO_SOURCE', 'detail': path})
        return res
    nontrans = [p[1] for p in paras if translate(p[2]) != p[2]]
    if nontrans:
        res['review'].append({'kind': 'DB_TYPOGRAPHY', 'detail': f'{len(nontrans)} paragraphs keep curly quotes/dashes'})
        for p in paras: p[2] = p[2]  # untouched; positions computed on the stored text
    ptext, praw, rawpos, ptok, numtok, heads = pdf_side(path, title)
    res['pdf_numbers'] = len(numtok)
    numbers_reliable = len(paras) > 1 and len(numtok) >= 0.9 * (len(paras) - 1) and \
        sorted(numtok.values()) == list(range(min(numtok.values()), max(numtok.values()) + 1)) if numtok else False
    res['numbers_reliable'] = numbers_reliable

    def run_pass(paras, collect):
        dtok = db_side(paras)
        ops = align([t for t, *_ in dtok], [t for t, *_ in ptok])
        edits = {}   # para index -> list of (start, end, replacement)
        num_at = {}  # db token index -> printed number starting there
        equal_num = []
        for op, i1, i2, j1, j2 in ops:
            if op == 'equal':
                for j in range(j1, j2):
                    if j in numtok:
                        equal_num.append((i1 + j - j1, numtok[j]))
                continue
            pidx = [j for j in range(j1, j2) if j in numtok]
            rest = [j for j in range(j1, j2) if j not in numtok]
            for j in pidx:
                num_at[i1 if (not rest or j < rest[0]) else i2] = numtok[j]
            d = dtok[i1:i2]
            p = [ptok[j] for j in rest]
            if not collect:
                continue
            dpar = {x[1] for x in d}
            ctx_par = paras[d[0][1]][1] if d else (paras[dtok[min(i1, len(dtok) - 1)][1]][1] if dtok else None)
            dstr = ' '.join(t for t, *_ in d)
            pstr = ' '.join(t for t, *_ in p)
            if not d and not p:
                continue
            contiguous = bool(p) and all(rest[k + 1] == rest[k] + 1 for k in range(len(rest) - 1))
            if d and p and len(dpar) == 1 and contiguous:
                pi = d[0][1]
                txt = paras[pi][2]
                dsub = txt[d[0][2]:d[-1][3]]
                psub = ptext[p[0][1]:p[-1][2]].replace('\n', ' ')
                psub = re.sub(r' {2,}', ' ', psub)
                if letters(dsub) == letters(psub) and words_known(psub, dsub, lang):
                    edits.setdefault(pi, []).append((d[0][2], d[-1][3], psub))
                    res['fixes'].append({'kind': 'SPACING', 'para': paras[pi][1], 'db': dsub, 'new': psub})
                    continue
                # em dash lost by the old importer: PDF chars equal once its dashes are removed, and every
                # extra dash on the PDF side is an em dash in the raw PDF text
                if letters(dsub).replace('-', '') == letters(psub).replace('-', ''):
                    seg_raw = praw[rawpos[p[0][1]]:rawpos[p[-1][2] - 1] + 1]
                    extra = psub.count('-') - dsub.count('-')
                    em = seg_raw.count('—') + seg_raw.count('–')
                    if extra > 0 and em >= extra and seg_raw.count('-') <= dsub.count('-'):
                        # an em dash wrapped at the end of a PDF line comes out of pdftotext as "— "
                        psub2 = re.sub(r'- (?=\w)', '-', psub)
                        edits.setdefault(pi, []).append((d[0][2], d[-1][3], psub2))
                        res['fixes'].append({'kind': 'EMDASH', 'para': paras[pi][1], 'db': dsub, 'new': psub2})
                        continue
            whole = d and len(dpar) == 1 and len([x for x in dtok if x[1] == d[0][1]]) == len(d)
            if d and not p and len(dpar) == 1 and is_header_like(d) and header_verified(d, heads, title) and                     (not whole or numbers_reliable):
                pi = d[0][1]
                edits.setdefault(pi, []).append((d[0][2], d[-1][3], None))
                res['fixes'].append({'kind': 'HEADER', 'para': paras[pi][1],
                                     'db': paras[pi][2][d[0][2]:d[-1][3]]})
                continue
            if numbers_reliable and not d and p and all(t.isdigit() for t, *_ in p) and len(p) == 1 and i1 < len(dtok) and \
                    i1 > 0 and dtok[i1][1] != dtok[i1 - 1][1]:
                # number swallowed as a paragraph number at a database paragraph boundary
                pi = dtok[i1 - 1][1]
                end = dtok[i1 - 1][3]
                edits.setdefault(pi, []).append((end, end, ' ' + p[0][0]))
                res['fixes'].append({'kind': 'NUMWORD', 'para': paras[pi][1], 'db': '', 'new': p[0][0]})
                continue
            if not d and p and i1 == 0 and rest[0] == 0 and contiguous and dtok and \
                    any(re.search(r'[a-zß-ÿ]', t) for t, *_ in p):
                # the opening words of paragraph 1, dropped by the old import together with the title
                psub = re.sub(r' {2,}', ' ', ptext[p[0][1]:p[-1][2]].replace('\n', ' '))
                edits.setdefault(0, []).append((0, 0, psub + ' '))
                res['fixes'].append({'kind': 'FIRSTLINE', 'para': paras[0][1], 'db': '', 'new': psub})
                continue
            if numbers_reliable and d and i1 == 0 and caps(dstr) and caps(dstr) in caps(title) and                     len(caps(dstr)) >= 0.6 * len(caps(title)):
                for pi in sorted(dpar):
                    ds = [x for x in d if x[1] == pi]
                    edits.setdefault(pi, []).append((ds[0][2], ds[-1][3], None))
                res['fixes'].append({'kind': 'TITLE', 'para': ctx_par, 'db': dstr})
                continue
            kind = 'DB_ONLY' if d and not p else 'PDF_ONLY' if p and not d else 'DIFF'
            if not d and p and all(not WORD.search(t) for t, *_ in p):
                kind = 'PUNCT_PDF'
            if d and not p and all(not WORD.search(t) for t, *_ in d):
                kind = 'PUNCT_DB'
            res['review'].append({'kind': kind, 'para': ctx_par, 'db': dstr[:300], 'pdf': pstr[:300]})
        return dtok, edits, num_at, equal_num

    dtok, edits, _, _ = run_pass(paras, True)
    # apply text edits right-to-left
    for pi, es in edits.items():
        txt = paras[pi][2]
        for s0, e0, rep in sorted(es, key=lambda x: -x[0]):
            if rep is None:
                left = txt[:s0].rstrip()
                right = txt[e0:].lstrip()
                txt = (left + ' ' + right).strip() if left and right else (left or right)
            else:
                txt = txt[:s0] + rep + txt[e0:]
        paras[pi][2] = txt
    # pass 2: segmentation from the printed numbers
    dtok, _, num_at, equal_num = run_pass(paras, False)
    drop = {idx for idx, _ in equal_num}
    db_starts = {}
    for idx, (t, pi, s0, e0) in enumerate(dtok):
        if idx == 0 or dtok[idx - 1][1] != pi:
            db_starts[idx] = paras[pi][1]
    want = dict(num_at)
    want.update({idx: n for idx, n in equal_num})
    first_num = min(numtok.values()) if numtok else None
    want_full = {0: 1 if first_num in (None, 2) else first_num}
    want_full.update(want)
    if equal_num and not numbers_reliable:
        res['review'].append({'kind': 'NUMBER_IN_TEXT', 'detail': str(equal_num[:10])})
    if numbers_reliable and (want_full != db_starts or drop):
        # rebuild paragraphs on the printed numbers
        cuts = sorted(want_full.items())
        newp = []
        for k, (start, num) in enumerate(cuts):
            end = cuts[k + 1][0] if k + 1 < len(cuts) else len(dtok)
            if end <= start:
                continue
            pieces = []
            cur_pi = None
            if start in drop:
                start += 1
            for idx in range(start, end):
                t, pi, s0, e0 = dtok[idx]
                if pi != cur_pi:
                    # span inside this old paragraph
                    last = idx
                    while last + 1 < end and dtok[last + 1][1] == pi:
                        last += 1
                    pieces.append((pi, paras[pi][2][s0:dtok[last][3]]))
                    cur_pi = pi
            text = ' '.join(x for _, x in pieces).strip()
            page = paras[pieces[0][0]][3]
            newp.append({'number': num, 'text': text, 'page': page})
        old_words = [t for idx, (t, *_) in enumerate(dtok) if idx not in drop]
        new_words = [t for p in newp for t, _, _ in tokens(p['text'])]
        nums = [p['number'] for p in newp]
        res['seg_ok'] = old_words == new_words and all(p['text'].strip() for p in newp) and \
            nums == list(range(nums[0], nums[0] + len(nums)))
        old_nums = [p[1] for p in paras]
        res['segment'] = {'old_count': len(paras), 'new_count': len(newp),
                          'old_numbers_head': old_nums[:3], 'new_numbers': [p['number'] for p in newp][:3],
                          'merged_or_split': sum(1 for k in db_starts if k not in want_full) +
                          sum(1 for k in want_full if k not in db_starts)}
        res['new_paragraphs'] = newp
    res['paragraph_ids'] = [p[0] for p in paras]
    res['edited'] = [{'id': paras[pi][0], 'text': paras[pi][2]} for pi in edits]
    # numbering check against the PDF
    printed = sorted(set(numtok.values()))
    res['pdf_number_gaps'] = [(a, b) for a, b in zip(printed, printed[1:]) if b != a + 1][:10]
    return res


def main():
    out, where = sys.argv[1], sys.argv[2]
    con = connect()
    ids = [r[0] for r in con.execute(f'select s.Id from Sermons s where {where} order by s.Id')]
    con.close()
    with ProcessPoolExecutor(max_workers=int(os.environ.get('MF_WORKERS', '10'))) as ex, \
            open(out, 'w', encoding='utf-8') as f:
        for i, r in enumerate(ex.map(analyse, [(x,) for x in ids], chunksize=1)):
            f.write(json.dumps(r, ensure_ascii=False) + '\n')
            f.flush()
            if i % 100 == 0:
                print(i, len(ids), flush=True)


if __name__ == '__main__':
    main()
