import json, re, os, sys, collections, sqlite3
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cmp_songs import slide_lines, ws, con

def strip(s): return re.sub(r'\s+', '', s.replace('\xad', ''))
out = open('corrections_songs_bible.jsonl', 'w', encoding='utf-8')
st = collections.Counter()

# Bible: restore from the original sources (see cmp_bible.py)
for vid, old_end, fix in ((91416, 'with your spirit.', ' Amen.'), (91571, 'in sincerity.', ' Amen.')):
    old = con.execute('select Text from BibleVerses where Id=?', (vid,)).fetchone()[0]
    assert old.endswith(old_end)
    out.write(json.dumps({'op': 'verse', 'id': vid, 'old': old, 'new': old + fix}, ensure_ascii=False) + '\n'); st['verse'] += 1
for vid, prefix in ((135014, '-79 '), (135073, '-18 ')):
    old = con.execute('select Text from BibleVerses where Id=?', (vid,)).fetchone()[0]
    assert old.startswith(prefix)
    out.write(json.dumps({'op': 'verse', 'id': vid, 'old': old, 'new': old[len(prefix):]}, ensure_ascii=False) + '\n'); st['verse'] += 1

# Songs (English, Kiswahili): line text from the slides, only where the characters are identical
for lang, root in (('en', r'E:\SONG PRESENTATION'), ('sw', r'E:\swahili-pptx')):
    for sid, fn, sp in con.execute('select Id, FileName, SourceFilePath from Songs where Language=? and IsActive=1', (lang,)).fetchall():
        path = re.sub(r'^[Dd]:', 'E:', sp)
        if not os.path.exists(path): path = os.path.join(root, fn)
        slides = [s for s in slide_lines(path) if s]
        secs = con.execute('select Id, SectionOrder, SectionType, SectionLabel, Text from SongSections where SongId=? order by SectionOrder', (sid,)).fetchall()
        if sid == 2927:
            # TARAJA YANGU: the old import put the chorus between the two halves of verse 1; follow the slides
            new = []
            for k, s in enumerate(slides, 1):
                chorus = s[0].lower().startswith('chœur') or s[0].lower().startswith('choeur')
                new.append({'type': 'Chorus' if chorus else 'Verse',
                            'label': ('Kiitikio - Slide %d' if chorus else 'Couplet - Slide %d') % k,
                            'text': '\r\n'.join(ws(l.replace('\xad', '')) for l in s)})
            out.write(json.dumps({'op': 'song', 'songId': sid, 'expectedIds': [x[0] for x in secs], 'sections': new}, ensure_ascii=False) + '\n')
            st['song'] += 1
            continue
        src = [ws(l.replace('\xad', '')) for s in slides for l in s]
        k = 0
        for secid, order, typ, lab, text in secs:
            lines = text.replace('\r\n', '\n').split('\n')
            newlines = []
            changed = False
            for l in lines:
                if not ws(l):
                    newlines.append(l); continue
                if k >= len(src):
                    raise SystemExit(f'song {sid}: more lines than slides')
                s = src[k]; k += 1
                if ws(l) != s:
                    assert strip(l) == strip(s), (sid, l, s)
                    newlines.append(s); changed = True
                else:
                    newlines.append(l)
            if changed:
                out.write(json.dumps({'op': 'section', 'id': secid, 'old': text, 'new': '\r\n'.join(newlines)}, ensure_ascii=False) + '\n')
                st['section_' + lang] += 1
        assert k == len(src), (sid, k, len(src))
print(st)
