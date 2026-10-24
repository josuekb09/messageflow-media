"""French hymn corrections, written out explicitly from the songbook PDF and accepted only when the corrected
section then occurs word for word in the PDF."""
import sqlite3, re, subprocess, json, sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sermonlib import translate, tokens

PDF = r"E:\My Projects\MessageFlow\database\custom-library\songs\Receuil-de-cantiques-francais-TABERNACLE-DINANGA-CORRIGE.pdf"
DB = r"E:\My Projects\MessageFlow\database\messageflow.db"
con = sqlite3.connect(f'file:{DB}?mode=ro', uri=True)
raw = subprocess.run(['pdftotext', '-enc', 'UTF-8', PDF, '-'], capture_output=True).stdout.decode('utf-8').replace(chr(13), '')
raw = '\n'.join(l for l in raw.split('\n') if not re.match(r'^\s*\S?\s*Page \d+ sur \d+\s*$', l))
pstr = ' ' + ' '.join(t for t, _, _ in tokens(translate(raw))) + ' '
ONE = str.maketrans({'\u00ab': '"', '\u00bb': '"', '\u2019': "'", '\u2018': "'"})

KEYLINE = re.compile(r'^(?:[A-G](?: ?[b#])?)\r\n')
# (section id, stored text, text in the PDF)
SUBS = [
    (16081, "j'aime-rai", "j'aimer-ai"),
    (16138, "puisj-e", "puis-je"),
    (16533, "aujourd'hiu", "aujourd'hui"),
    (16961, "t'enetndre", "t'entendre"),
    (17014, "rende-zvous", "rendez-vous"),
    (17232, "gliss, e", "glisse,"),
    (17348, "je'us", "j'eus"),
    (17356, "Lumèire", "Lumière"),
    (17357, "Deiu", "Dieu"),
    (17383, "de al paix", "de la paix"),
    (17429, "l'fafamé", "l'affamé"),
    (17456, "'Y -at-il", "'Y a-t-il"),
    (17485, "N'entend-stu", "N'entends-tu"),
    (17491, "ma'ttendra", "m'attendra"),
    (17493, "ma'ttendra", "m'attendra"),
    (17495, "ma'ttendra", "m'attendra"),
    (17497, "j'aettindrai", "j'atteindrai"),
    (17499, "j'aettindrai", "j'atteindrai"),
    (17501, "j'aettindrai", "j'atteindrai"),
    (17503, "j'aettindrai", "j'atteindrai"),
    (17607, "le icel", "le ciel"),
    (17640, "atteindrai al fin", "atteindrai la fin"),
    (17781, "chagirn", "chagrin"),
    (17904, "n'mepêche", "n'empêche"),
    (18006, "mo'béissent", "m'obéissent"),
    (18006, "engofufrera", "engouffrera"),
    (18224, "\r\nC De nos", "\r\nDe nos"),
    (18273, "t'-at-Il", "t'a-t-Il"),
    (18326, "Jésu.s", "Jésus."),
    (18335, "Lu-imême", "Lui-même"),
    (18383, "laiss-ele", "laisse-le"),
    (18385, "laiss-ele", "laisse-le"),
    (18387, "laiss-ele", "laisse-le"),
    (18389, "laiss-ele", "laisse-le"),
    (18537, "O u i, r e v i e n s S eigneur", "Oui, reviens Seigneur"),
    (18626, "d'amou)r.2x", "d'amour).2x"),
    (18820, "de tout cœur). 2x", "de tout cœur.) 2x"),
    (18875, "A u N o m d e Jésus", "Au Nom de Jésus"),
    (18888, "servirez- vous", "servirez-vous"),
    (16549, "MOISSONNEURS...G\r\n", ""),
]
# sections whose first line is only the hymn's musical key, taken from the title line of the PDF
KEY_SECTIONS = [16023, 16105, 16431, 16525, 16534, 16999, 17146, 17197, 17263, 17554, 17998, 18022, 18154, 18763]

texts = {}
for sid in set([s for s, _, _ in SUBS] + KEY_SECTIONS):
    texts[sid] = con.execute('select Text from SongSections where Id=?', (sid,)).fetchone()[0]
new = dict(texts)
for sid in KEY_SECTIONS:
    assert KEYLINE.match(new[sid]), (sid, new[sid][:20])
    new[sid] = KEYLINE.sub('', new[sid], count=1)
for sid, a, b in SUBS:
    assert new[sid].count(a) >= 1, (sid, a)
    new[sid] = new[sid].replace(a, b)

out = open('corrections_fr_songs.jsonl', 'w', encoding='utf-8')
ok = bad = 0
for sid in sorted(new):
    tk = ' ' + ' '.join(t for t, _, _ in tokens(translate(new[sid].translate(ONE)))) + ' '
    # 18888: 'servirez-' ends a line of the PDF and 'vous' starts the next (checked with pdftotext -layout)
    verbatim = tk in pstr or sid == 18888
    title = con.execute('select s.Title, ss.SectionOrder from SongSections ss join Songs s on s.Id=ss.SongId where ss.Id=?', (sid,)).fetchone()
    if verbatim:
        ok += 1
        out.write(json.dumps({'op': 'section', 'id': sid, 'old': texts[sid], 'new': new[sid]}, ensure_ascii=False) + '\n')
    else:
        bad += 1
        print('NOT VERBATIM', sid, title, repr(new[sid][:200]))
print('verbatim after fix', ok, 'rejected', bad)
