import sqlite3, collections, pickle, re, sys
sys.path.insert(0, __import__('os').path.dirname(__import__('os').path.abspath(__file__)))
from sermonlib import tokens, DB
con = sqlite3.connect(f'file:{DB}?mode=ro', uri=True)
vocab = collections.defaultdict(collections.Counter)
for lang, t in con.execute('select s.Language, p.Text from SermonParagraphs p join Sermons s on s.Id=p.SermonId'):
    for tok, _, _ in tokens(t):
        vocab[lang][tok.lower()] += 1
pickle.dump(dict(vocab), open(__import__('os').path.join(__import__('os').path.dirname(__import__('os').path.abspath(__file__)), 'vocab.pkl'), 'wb'))
print({k: len(v) for k, v in vocab.items()})
