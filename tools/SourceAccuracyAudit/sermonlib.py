"""Shared helpers: independent PDF reading (Poppler pdftotext), tokenising with character spans, and a fast
token alignment (git's C diff) that yields difflib-style opcodes."""
import os, re, subprocess, tempfile, uuid, sqlite3

DB = os.environ.get('MF_DB', r"E:\My Projects\MessageFlow\database\messageflow.db")
TMP = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'tmp_align')
os.makedirs(TMP, exist_ok=True)

TYPO = {'\u2019': "'", '\u2018': "'", '\u201a': "'", '\u201b': "'", '\u201c': '"', '\u201d': '"', '\u201e': '"',
        '\u201f': '"', '\u2014': '-', '\u2013': '-', '\u2010': '-', '\u2011': '-', '\u2012': '-', '\u2212': '-',
        '\u00a0': ' ', '\u202f': ' ', '\u2009': ' ', '\u00ad': '', '\u2026': '...', '\ufeff': '', '\u2032': "'",
        '\u02bc': "'", '\ufb01': 'fi', '\ufb02': 'fl', '\u00ab': '"', '\u00bb': '"'}
TYPO_T = str.maketrans(TYPO)
TOKEN = re.compile(r'[-"\'.,;:!?()\[\]]|[^\s\-"\'.,;:!?()\[\]]+')
WORD = re.compile(r'\w')


def translate(s):
    return s.translate(TYPO_T)


def tokens(s):
    """[(token, start, end)] on the given (already translated) string."""
    return [(m.group(0), m.start(), m.end()) for m in TOKEN.finditer(s)]


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


TRAILER = re.compile(r'ALL RIGHTS RESERVED|Copyright Notice|Copyright notice|TOUS DROITS|HAKI ZOTE|Avis de droits|'
                     r'VOICE OF GOD RECORDINGS|Ujumbe huu (?:wa|uliohubiriwa na) Ndugu William Marrion Branham|'
                     r'Jumbe hiz[ao] za Ndugu William|Ce Message de Frère William Marrion Branham|'
                     r'Ces Messages de Frère William Marrion Branham')


def pdf_body(path):
    """(body text with furniture lines removed, list of removed furniture lines, raw page count)"""
    r = subprocess.run(['pdftotext', '-enc', 'UTF-8', path, '-'], capture_output=True)
    lines = r.stdout.decode('utf-8', 'replace').split('\n')
    for k in range(len(lines)):
        if TRAILER.search(lines[k]) and k > len(lines) * 0.5:
            lines = lines[:k]
            break
    body, furn = [], []
    for l in lines:
        (furn if is_furniture(l) else body).append(l)
    return '\n'.join(body), [l.strip() for l in furn if l.strip()]


def align(a, b):
    """difflib-style opcodes for token lists a, b using git diff (Myers, C)."""
    key = uuid.uuid4().hex
    fa = os.path.join(TMP, key + '.a')
    fb = os.path.join(TMP, key + '.b')
    with open(fa, 'w', encoding='utf-8', newline='\n') as f:
        f.write('\n'.join(a) + '\n')
    with open(fb, 'w', encoding='utf-8', newline='\n') as f:
        f.write('\n'.join(b) + '\n')
    r = subprocess.run(['git', 'diff', '--no-index', '--no-color', '-U0', '--minimal', fa, fb], capture_output=True)
    os.remove(fa); os.remove(fb)
    hunks = []
    for line in r.stdout.decode('utf-8', 'replace').split('\n'):
        m = re.match(r'^@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@', line)
        if m:
            a0 = int(m.group(1)); al = 1 if m.group(2) is None else int(m.group(2))
            b0 = int(m.group(3)); bl = 1 if m.group(4) is None else int(m.group(4))
            i1 = a0 - 1 if al > 0 else a0
            j1 = b0 - 1 if bl > 0 else b0
            hunks.append((i1, i1 + al, j1, j1 + bl))
    ops = []
    pi = pj = 0
    for i1, i2, j1, j2 in hunks:
        if i1 > pi or j1 > pj:
            ops.append(('equal', pi, i1, pj, j1))
        op = 'replace' if (i2 > i1 and j2 > j1) else ('delete' if i2 > i1 else 'insert')
        ops.append((op, i1, i2, j1, j2))
        pi, pj = i2, j2
    if pi < len(a) or pj < len(b):
        ops.append(('equal', pi, len(a), pj, len(b)))
    return ops


def connect():
    return sqlite3.connect(f'file:{DB}?mode=ro', uri=True)


def source_path(p):
    return re.sub(r'^[Dd]:', 'E:', p)
