"""
Lists the methods still written in the style of the translated asm (at least 2 lines with goto L, FlagSet, Compare..., SumWord, ...),
shortest first: the candidates for the next rewrite.

usage (from the repository root): python Civilization1Remaster.Tests/Differential/Reference/find_asm_methods.py Civilization1Remaster.Core [count]
columns: lines, asm-style lines, gotos, file, method, line
"""
import re, sys, pathlib
root = pathlib.Path(sys.argv[1])
markers = re.compile(r'goto L|FlagSet|CompareWord|CompareByte|ScaleIndex|ShiftLeftWord|SumWord|SubtractWord|MultiplySignedWord|AndWord|OrWord|IncreaseWord|DecreaseWord|NegateWord|XorWord')
res = []
for f in root.rglob('*.cs'):
    if '\\obj\\' in str(f) or '/obj/' in str(f): continue
    lines = f.read_text(encoding='utf-8', errors='replace').splitlines()
    i = 0
    while i < len(lines):
        m = re.match(r'\t\t(public|private|internal)\s+[\w<>\[\], ]+\s+(\w+)\(', lines[i])
        if m:
            name = m.group(2)
            # find end: next line == '\t\t}'
            j = i + 1
            while j < len(lines) and lines[j] != '\t\t}':
                j += 1
            body = lines[i:j+1]
            cnt = sum(1 for l in body if markers.search(l))
            gotos = sum(1 for l in body if 'goto L' in l)
            if cnt >= 2 and 'CRuntime' not in f.name and 'SoundDriver' not in f.name:
                res.append((len(body), cnt, gotos, f.name, name, i+1))
            i = j
        i += 1
res.sort()
for r in res[:int(sys.argv[2]) if len(sys.argv) > 2 else 80]:
    print(*r)
print('total methods', len(res))
