import json, sqlite3, sys, collections
DB = r"E:\My Projects\MessageFlow\database\messageflow.db"
con = sqlite3.connect(f'file:{DB}?mode=ro', uri=True)
rows = [json.loads(l) for l in open('plan_all.jsonl', encoding='utf-8')]
out = open('corrections_sermons.jsonl', 'w', encoding='utf-8')
stats = collections.Counter(); skipped = []
for r in rows:
    if r['segment']:
        if not r.get('seg_ok'):
            skipped.append((r['id'], r['title'])); continue
        out.write(json.dumps({'op': 'sermon', 'sermonId': r['id'], 'expectedIds': r['paragraph_ids'],
                              'paragraphs': r['new_paragraphs']}, ensure_ascii=False) + '\n')
        stats['sermon'] += 1; stats['sermon_paras'] += len(r['new_paragraphs'])
        continue
    for e in r['edited']:
        old = con.execute('select Text from SermonParagraphs where Id=?', (e['id'],)).fetchone()[0]
        if old == e['text']:
            continue
        if not e['text'].strip():
            skipped.append((r['id'], 'empty para')); continue
        out.write(json.dumps({'op': 'paragraph', 'id': e['id'], 'old': old, 'new': e['text']}, ensure_ascii=False) + '\n')
        stats['paragraph'] += 1
print(stats, 'skipped', len(skipped)); print(skipped[:12])
