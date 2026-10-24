import os, sys, json, re, collections
from concurrent.futures import ProcessPoolExecutor
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import repair_sermons as rs
from sermonlib import tokens, align, connect, source_path, WORD

def one(sid):
    con = connect()
    title, spath, lang, author = con.execute('select s.Title, s.SourceFilePath, s.Language, a.DisplayName from Sermons s join Authors a on a.Id=s.AuthorId where s.Id=?', (sid,)).fetchone()
    paras = [t for (t,) in con.execute('select Text from SermonParagraphs where SermonId=? order by ParagraphNumber', (sid,))]
    ptext, praw, rawpos, ptok, numtok, heads = rs.pdf_side(source_path(spath), title)
    d = [t for p in paras for t, _, _ in tokens(p) if WORD.search(t)]
    p = [t for j, (t, _, _) in enumerate(ptok) if WORD.search(t) and j not in numtok]
    ops = align(d, p)
    eq = sum(i2 - i1 for op, i1, i2, j1, j2 in ops if op == 'equal')
    return (author, lang), len(d), eq, len(p)

if __name__ == '__main__':
    con = connect()
    ids = [r[0] for r in con.execute('select Id from Sermons')]
    agg = collections.defaultdict(lambda: [0, 0, 0, 0])
    with ProcessPoolExecutor(max_workers=int(os.environ.get('MF_WORKERS', '6'))) as ex:
        for g, nd, eq, npdf in ex.map(one, ids, chunksize=2):
            a = agg[g]; a[0] += 1; a[1] += nd; a[2] += eq; a[3] += npdf
    for g, (n, nd, eq, npdf) in sorted(agg.items()):
        print(g, f'docs={n} db_words={nd:,} pdf_words={npdf:,} db_words_identical_in_order={eq:,} ({100*eq/nd:.3f}%) db_words_not_in_pdf={nd-eq:,}')
