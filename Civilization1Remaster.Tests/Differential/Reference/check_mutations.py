"""
The mutation check of a batch: every deliberate mistake must make its differential test fail.

usage (from the repository root):
  python Civilization1Remaster.Tests/Differential/Reference/check_mutations.py <mutations.json> [--jobs N]
mutations.json: [{"file": "Civilization1Remaster.Core/...cs", "old": "...", "new": "...", "test": "SomeFunctionMatchesOriginalCode"}, ...]
("old" must occur exactly once in the file; "test" is the test method name, run as -method "*<test>")

The mutants run in parallel (--jobs, default 4), each worker in its own git worktree under .mutants/ (kept between runs, so
the builds there are incremental; .mutants/ is listed in .git/info/exclude). Every worker starts from the current work: HEAD
plus the uncommitted changes and the new files of the working tree. The working tree itself is never touched, so the work
can go on while the mutants run. Everything runs in Release.
Prints CAUGHT / SURVIVED / BUILD ERROR per mutation; exit code 1 when one was not caught.
A hanging scenario fails its test within DifferentialPair.RunSeconds (CAUGHT); a test process that still runs longer than
TIMEOUT seconds is killed with its child processes and counts as CAUGHT (hang).
A survivor can be an equivalent mutation (a value the code never reaches): then choose a mutation that changes behaviour.
"""
import json, os, re, shutil, subprocess, sys, threading
if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

ROOT = os.getcwd()
WORKTREES = os.path.join(ROOT, '.mutants')
EXE = os.path.join('Civilization1Remaster.Tests', 'bin', 'Release', 'net10.0', 'Civilization1Remaster.Tests.exe')
# Seconds a test process may run before it is killed (the harness itself stops a hanging scenario after 120 s)
TIMEOUT = 400


def run(args, cwd=ROOT, timeout=None):
    """Runs a process; on timeout kills it with all its children (taskkill /T) and returns None"""
    process = subprocess.Popen(args, cwd=cwd, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True,
                               encoding='utf-8', errors='replace')
    try:
        out, _ = process.communicate(timeout=timeout)
        return out
    except subprocess.TimeoutExpired:
        subprocess.run(['taskkill', '/PID', str(process.pid), '/T', '/F'], capture_output=True)
        process.communicate()
        return None


def build(tree):
    out = run(['dotnet', 'build', 'Civilization1Remaster.Tests', '-c', 'Release', '-v', 'q', '-nologo'], cwd=tree)
    return sorted(set(l.strip() for l in (out or '').splitlines() if ' error ' in l))


def prepare(tree, patch, untracked):
    """The worktree at HEAD plus the work of the working tree"""
    if not os.path.isdir(tree):
        subprocess.run(['git', 'worktree', 'add', '--detach', tree, 'HEAD'], cwd=ROOT, capture_output=True, check=True)
    else:
        subprocess.run(['git', 'checkout', '--detach', '-f', 'HEAD'], cwd=tree, capture_output=True, check=True)
        head = subprocess.run(['git', 'rev-parse', 'HEAD'], cwd=ROOT, capture_output=True, text=True).stdout.strip()
        subprocess.run(['git', 'reset', '--hard', head], cwd=tree, capture_output=True, check=True)
        subprocess.run(['git', 'clean', '-fdq'], cwd=tree, capture_output=True, check=True)

    if patch:
        applied = subprocess.run(['git', 'apply', '--whitespace=nowarn', '-'], cwd=tree, input=patch, capture_output=True,
                                 text=True, encoding='utf-8')
        assert applied.returncode == 0, applied.stderr

    for name in untracked:
        target = os.path.join(tree, name)
        os.makedirs(os.path.dirname(target), exist_ok=True)
        shutil.copy2(os.path.join(ROOT, name), target)


def check(tree, m):
    path = os.path.join(tree, m['file'])
    text = open(path, encoding='utf-8-sig').read()
    count = text.count(m['old'])

    if count != 1:
        return f"NOT APPLIED ({count} occurrences)  {m['test']}: {m['old']!r}", True

    had_bom = open(path, 'rb').read(3) == b'\xef\xbb\xbf'
    encoding = 'utf-8-sig' if had_bom else 'utf-8'
    open(path, 'w', encoding=encoding, newline='').write(text.replace(m['old'], m['new']))

    try:
        errors = build(tree)

        if errors:
            return f"BUILD ERROR  {m['test']}: {errors[0]}", True

        out = run([os.path.join(tree, EXE), '-method', '*' + m['test']], cwd=tree, timeout=TIMEOUT)

        if out is None:
            return f"CAUGHT (hang) {m['test']}", False

        total = re.search(r'Total: (\d+), Errors: (\d+), Failed: (\d+)', out)

        if total is None or total.group(1) == '0':
            return f"NO TEST RUN  {m['test']}", True

        if int(total.group(3)) > 0 or int(total.group(2)) > 0:
            return f"CAUGHT       {m['test']}", False

        return f"SURVIVED     {m['test']}: {m['old']!r} -> {m['new']!r}", True
    finally:
        open(path, 'w', encoding=encoding, newline='').write(text)


def main():
    mutations = json.load(open(sys.argv[1], encoding='utf-8'))
    jobs = int(sys.argv[sys.argv.index('--jobs') + 1]) if '--jobs' in sys.argv else 4
    jobs = max(1, min(jobs, len(mutations)))

    patch = subprocess.run(['git', 'diff', 'HEAD', '--binary'], cwd=ROOT, capture_output=True, text=True, encoding='utf-8').stdout
    untracked = [name for name in subprocess.run(['git', 'ls-files', '--others', '--exclude-standard'], cwd=ROOT,
                                                 capture_output=True, text=True).stdout.splitlines() if name.endswith('.cs')]

    exclude = os.path.join(ROOT, '.git', 'info', 'exclude')
    if '.mutants/' not in open(exclude, encoding='utf-8').read():
        open(exclude, 'a', encoding='utf-8').write('\n.mutants/\n')

    trees = [os.path.join(WORKTREES, f'w{idx}') for idx in range(1, jobs + 1)]
    results = [None] * len(mutations)
    lock = threading.Lock()

    def worker(slot):
        tree = trees[slot]
        prepare(tree, patch, untracked)
        errors = build(tree)

        for idx in range(slot, len(mutations), jobs):
            if errors:
                line, failed = f"BUILD ERROR (unchanged work) {mutations[idx]['test']}: {errors[0]}", True
            else:
                line, failed = check(tree, mutations[idx])

            results[idx] = (line, failed)

            with lock:
                print(line, flush=True)

    threads = [threading.Thread(target=worker, args=(slot,)) for slot in range(jobs)]

    for thread in threads:
        thread.start()

    for thread in threads:
        thread.join()

    failed = any(result is None or result[1] for result in results)
    caught = sum(1 for result in results if result is not None and not result[1])
    print(f'{caught} of {len(mutations)} caught, {jobs} jobs')
    sys.exit(1 if failed else 0)


main()
