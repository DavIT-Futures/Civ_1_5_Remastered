"""
Compares two reports of LeftoverWatcher (the variable CIV_LEFTOVERS, see LeftoverWatcher.cs): the readers of leftover
words that got other values between the reference run (before a change) and the new run (after it), with the writers
that were lost or came new (where to put the write back).

    python compare_leftovers.py before.txt after.txt

A reader is "Owner 0xNN" (the word NN of the frame of Owner read before Owner wrote it), a writer "Owner bp-0xNN" (the
last write of that stack word), "Owner push" (a push or a word below the frames) or "nobody". A reader that gets the
same values from another writer is fine. Lost writers of a reader with other values mean that the changed code no longer
leaves the word the reader used to get: the write has to stay (on the stack, next to the C# variable). Readers missing in
the new run read C# variables now (or the tests no longer reach them); they are listed apart. Run both reports on the same
tests (the golden master: -namespace Civilization1Remaster.Tests.GoldenMaster).
"""
import sys
from collections import defaultdict


def read(route):
    writers = defaultdict(lambda: defaultdict(int))
    values = defaultdict(set)
    with open(route, encoding='utf-8') as report:
        for line in report:
            reader, writer, value, tally = line.rstrip('\n').split('\t')
            writers[reader][writer] += int(tally)
            values[reader].add(value)
    return writers, values


def main():
    (before, before_values), (after, after_values) = read(sys.argv[1]), read(sys.argv[2])
    changed = 0

    for reader in sorted(before):
        if reader not in after or before_values[reader] == after_values[reader]:
            continue
        changed += 1
        print(f'{reader}: values {" ".join(sorted(before_values[reader]))} -> {" ".join(sorted(after_values[reader]))}')
        for writer in sorted(set(before[reader]) - set(after[reader])):
            print(f'    lost {writer} ({before[reader][writer]}x)')
        for writer in sorted(set(after[reader]) - set(before[reader])):
            print(f'    new  {writer} ({after[reader][writer]}x)')

    missing = sorted(reader for reader in before if reader not in after)
    if missing:
        print('\nReaders missing in the new run (C# variables now, or not reached):')
        for reader in missing:
            print(f'    {reader}: {", ".join(sorted(before[reader]))}')

    print(f'\n{changed} readers with other values, {len(missing)} readers missing')
    return 1 if changed else 0


if __name__ == '__main__':
    sys.exit(main())
