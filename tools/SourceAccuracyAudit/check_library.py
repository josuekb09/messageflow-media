"""Library-wide automated checks on every text that can reach the projector."""
import sqlite3, re, sys, os, collections
DB = os.environ.get('MF_DB', r"E:\My Projects\MessageFlow\database\messageflow.db")
con = sqlite3.connect(f'file:{DB}?mode=ro', uri=True)
out = collections.OrderedDict()

PLACEHOLDER = re.compile(r'lorem|ipsum|placeholder|sample text|\bTODO\b|FIXME|\bdummy\b|test paragraph|xxxxx|\bNULL\b|\bundefined\b|\bNaN\b', re.I)
HEADS = re.compile(r'THE SPOKEN WORD|THESPOKENWORD|LILENENOLILILONENWA|LILE N ?ENO LILILON ?EN ?WA|LAPAROLEPARL[EÉ]E|LA PAROLE PARL[EÉ]E')
RUNHEAD = re.compile(r"(?:\b[A-ZÀ-Ý][A-ZÀ-Ý'’,\-]*\s+){3,}[A-ZÀ-Ý'’,\-]*\.\.\.\s*\d{1,4}\b")
LEADNUM = re.compile(r'^\d{1,4}(?:\s|\.\.\.)')


def lang_of():
    return {sid: (lang, a) for sid, lang, a in con.execute(
        'select s.Id, s.Language, a.DisplayName from Sermons s join Authors a on a.Id=s.AuthorId')}


meta = lang_of()
c = collections.Counter()
ex = collections.defaultdict(list)
prev = {}
nums = collections.defaultdict(list)
for pid, sid, n, t in con.execute('select Id, SermonId, ParagraphNumber, Text from SermonParagraphs order by SermonId, ParagraphNumber'):
    g = meta[sid]
    nums[sid].append(n)
    c[g, 'paragraphs'] += 1
    for name, bad in (('empty', not t.strip()), ('placeholder', bool(PLACEHOLDER.search(t))),
                      ('page_head_glued', bool(HEADS.search(t))), ('running_head_with_page_no', bool(RUNHEAD.search(t))),
                      ('starts_with_number', bool(LEADNUM.match(t))),
                      ('same_as_previous', prev.get(sid) == t)):
        if bad:
            c[g, name] += 1
            if len(ex[g, name]) < 4:
                ex[g, name].append((sid, n, t[:100]))
    prev[sid] = t
for sid, ns in nums.items():
    g = meta[sid]
    if ns != list(range(ns[0], ns[0] + len(ns))):
        c[g, 'sermons_with_number_gaps'] += 1
        if len(ex[g, 'sermons_with_number_gaps']) < 4:
            ex[g, 'sermons_with_number_gaps'].append((sid, [(a, b) for a, b in zip(ns, ns[1:]) if b != a + 1][:3]))
    if ns[0] != 1:
        c[g, 'not_starting_at_1'] += 1
c['sermons_total'] = len(nums)

for tid, name in con.execute('select Id, Abbreviation from BibleTranslations'):
    rows = con.execute('select BookId, Chapter, Verse, Text from BibleVerses where TranslationId=?', (tid,)).fetchall()
    c[name, 'verses'] = len(rows)
    c[name, 'empty'] = sum(1 for r in rows if not r[3].strip())
    c[name, 'placeholder'] = sum(1 for r in rows if PLACEHOLDER.search(r[3]))
    c[name, 'leading_number'] = sum(1 for r in rows if re.match(r'^-?\d', r[3]))
    for r in rows:
        if re.match(r'^-?\d', r[3]) and len(ex[name, 'leading_number']) < 5:
            ex[name, 'leading_number'].append(r[:3] + (r[3][:60],))

for lang, in con.execute('select distinct Language from Songs'):
    rows = con.execute('select s.Id, s.Title, ss.SectionOrder, ss.Text from SongSections ss join Songs s on s.Id=ss.SongId '
                       'where s.Language=? and s.IsActive=1', (lang,)).fetchall()
    c['songs_' + lang, 'sections'] = len(rows)
    c['songs_' + lang, 'songs'] = len({r[0] for r in rows})
    c['songs_' + lang, 'empty'] = sum(1 for r in rows if not r[3].strip())
    c['songs_' + lang, 'placeholder'] = sum(1 for r in rows if PLACEHOLDER.search(r[3]))
    key = [r for r in rows if r[2] == 1 and re.match(r'^[A-G](?: ?[b#])?\r?\n', r[3])]
    c['songs_' + lang, 'key_line_at_start'] = len(key)
    for r in key[:4]: ex['songs_' + lang, 'key_line_at_start'].append(r[:3])
    nosongs = con.execute('select count(*) from Songs s where Language=? and IsActive=1 and not exists '
                          '(select 1 from SongSections ss where ss.SongId=s.Id)', (lang,)).fetchone()[0]
    c['songs_' + lang, 'songs_without_sections'] = nosongs

for k, v in c.items():
    print(k, v)
print('--- examples')
for k, v in ex.items():
    print(k, v)
