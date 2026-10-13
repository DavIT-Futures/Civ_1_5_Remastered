"""usage: replace_methods.py <target .cs> <file with new methods>
Each new method (with its /// comment) replaces the method of the same name in the target (from its /// comment to '\t\t}').
A method the target does not have yet (a new helper) is inserted after the method before it in the new file, with an empty
line between them."""
import re, sys
target, new = sys.argv[1:3]
text = open(target, encoding='utf-8-sig').read().replace('\r\n', '\n').split('\n')
had_bom = open(target, 'rb').read(3) == b'\xef\xbb\xbf'
new_lines = open(new, encoding='utf-8').read().replace('\r\n', '\n').rstrip('\n').split('\n')

# split new into methods
methods = []
cur = []
for line in new_lines:
    cur.append(line)
    if line == '\t\t}':
        methods.append(cur)
        cur = []
        continue
previous_end = None
for m in methods:
    while m and m[0].strip() == '':
        m.pop(0)
    sig = next(l for l in m if re.match(r'^\t\t(public|private|internal) ', l))
    name = re.search(r'\s(\w+)\(', sig).group(1)
    starts = [i for i, l in enumerate(text) if re.match(rf'^\t\t(public|private|internal) .*\s{name}\(', l)]
    if not starts:
        assert previous_end is not None, (name, 'a new method needs a replaced method before it in the new file')
        text[previous_end + 1:previous_end + 1] = [''] + m
        previous_end += len(m) + 1
        print('inserted', name)
        continue
    assert len(starts) == 1, (name, starts)
    start = starts[0]
    while text[start - 1].strip().startswith('///'):
        start -= 1
    end = next(i for i in range(start, len(text)) if text[i] == '\t\t}')
    text[start:end + 1] = m
    previous_end = start + len(m) - 1
    print('replaced', name)
with open(target, 'w', encoding='utf-8-sig' if had_bom else 'utf-8', newline='\r\n') as f:
    f.write('\n'.join(text))
