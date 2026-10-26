# Ligature fix log (Frank) — DRY RUN, 2026-10-26

Database untouched. Method: each Frank paragraph aligned word-by-word against pypdf text; a fix needs PDF word = stored word(s) + only missing f/i/l/t letters, neighbours identical, rest matching.

- Frank sermons checked: 85 (9,516 paragraphs); PDFs read by pypdf: 66; 19 PDFs are AES-encrypted and unreadable here (pypdf needs `cryptography`)
- Fixes available: 0
- Ambiguous: 0
- Junk page-number paragraphs: 0 (no stored paragraph is a bare number)
- Direct DB scan for lost-ligature signatures ("a er", "con rmation", "o ended"...): none found in Frank text.
- Sample fixes: none exist.
