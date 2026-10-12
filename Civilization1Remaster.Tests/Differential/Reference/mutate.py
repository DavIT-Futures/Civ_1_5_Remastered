"""
A deliberate mistake for the mutation check: replaces the only occurrence of a text in a file.
usage: python mutate.py <file> <old text> <new text>   (the text must occur exactly once)
"""
import sys
p, old, new = sys.argv[1:4]
t = open(p, encoding='utf-8-sig').read()
assert t.count(old) == 1, (p, old, t.count(old))
open(p, 'w', encoding='utf-8', newline='').write(t.replace(old, new))
print('mutated', p)
