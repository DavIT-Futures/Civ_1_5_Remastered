"""
The mutation check of a batch: every deliberate mistake must make its differential test fail.

usage (from the repository root): python Civilization1Remaster.Tests/Differential/Reference/check_mutations.py <mutations.json>
mutations.json: [{"file": "Civilization1Remaster.Core/...cs", "old": "...", "new": "...", "test": "SomeFunctionMatchesOriginalCode"}, ...]
("old" must occur exactly once in the file; "test" is the test method name, run as -method "*<test>")

The uncommitted work of Civilization1Remaster.Core and Civilization1Remaster.Machine is saved as a patch first and put back
after every mutation (git checkout + git apply), then the test project is built again from the unchanged work.
Everything runs in Release (about 7 times faster than Debug, same results).
Prints CAUGHT / SURVIVED / BUILD ERROR per mutation; exit code 1 when one was not caught.
A test that runs longer than TIMEOUT seconds (a hanging mutant) counts as CAUGHT (hang).
A survivor can be an equivalent mutation (a value the code never reaches): then choose a mutation that changes behaviour.
"""
import json, os, re, subprocess, sys, tempfile

SOURCES = ['Civilization1Remaster.Core', 'Civilization1Remaster.Machine']
EXE = os.path.join('Civilization1Remaster.Tests', 'bin', 'Release', 'net10.0', 'Civilization1Remaster.Tests.exe')
# Seconds a test may run before the mutant counts as hanging (caught)
TIMEOUT = 600


def run(args, **kwargs):
    return subprocess.run(args, capture_output=True, text=True, encoding='utf-8', errors='replace', **kwargs)


def build():
    out = run(['dotnet', 'build', 'Civilization1Remaster.Tests', '-c', 'Release', '-v', 'q', '-nologo'])
    errors = sorted(set(l.strip() for l in out.stdout.splitlines() if ' error ' in l))
    return errors


def main():
    mutations = json.load(open(sys.argv[1], encoding='utf-8'))
    patch = os.path.join(tempfile.gettempdir(), 'civ1-mutation-work.patch')
    diff = run(['git', 'diff', '--binary', '--'] + SOURCES)
    open(patch, 'w', encoding='utf-8', newline='').write(diff.stdout)
    before = run(['git', 'diff', '--stat', '--'] + SOURCES).stdout
    failed = False

    try:
        for m in mutations:
            text = open(m['file'], encoding='utf-8-sig').read()
            count = text.count(m['old'])

            if count != 1:
                print(f"NOT APPLIED ({count} occurrences)  {m['test']}: {m['old']!r}")
                failed = True
                continue

            had_bom = open(m['file'], 'rb').read(3) == b'\xef\xbb\xbf'
            open(m['file'], 'w', encoding='utf-8-sig' if had_bom else 'utf-8', newline='').write(text.replace(m['old'], m['new']))

            try:
                errors = build()

                if errors:
                    print(f"BUILD ERROR  {m['test']}: {errors[0]}")
                    failed = True
                    continue

                try:
                    out = run([EXE, '-method', '*' + m['test']], timeout=TIMEOUT).stdout
                except subprocess.TimeoutExpired:
                    # A test that hangs (an endless loop of the mutant) catches the mutation too
                    print(f"CAUGHT (hang) {m['test']}", flush=True)
                    continue

                total = re.search(r'Total: (\d+), Errors: (\d+), Failed: (\d+)', out)

                if total is None or total.group(1) == '0':
                    print(f"NO TEST RUN  {m['test']}")
                    failed = True
                elif int(total.group(3)) > 0 or int(total.group(2)) > 0:
                    print(f"CAUGHT       {m['test']}")
                else:
                    print(f"SURVIVED     {m['test']}: {m['old']!r} -> {m['new']!r}")
                    failed = True
            finally:
                run(['git', 'checkout', '--'] + SOURCES)

                if os.path.getsize(patch) > 0:
                    applied = run(['git', 'apply', '--whitespace=nowarn', patch])
                    assert applied.returncode == 0, applied.stderr
    finally:
        after = run(['git', 'diff', '--stat', '--'] + SOURCES).stdout
        assert before == after, 'the work was not restored:\n' + before + '\n---\n' + after
        errors = build()
        print('work restored, build ' + ('ERRORS: ' + errors[0] if errors else 'ok'))

    sys.exit(1 if failed else 0)


main()
