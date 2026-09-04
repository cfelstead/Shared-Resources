---
name: cfd-implement
description: Step 6 of the cfd workflow. Implement one ticket at a time test-first (red before green) at its declared seam, holding the implementation tightly to SCHEMA.md and UI-DECISIONS.md, then commit and hand off to cfd-code-review. Also used to apply code-review fixes and re-trigger review. Use after cfd-tickets.
disable-model-invocation: true
---

## Purpose

This is where the schema, UI, and spec decisions from earlier steps actually become code. The discipline here has two jobs: build strictly test-first so coverage is real rather than retrofitted, and hold the implementation to what was already agreed rather than letting it drift — a column renamed on the fly, a UI variant "improved" beyond what was decided, or a test written against internals instead of the agreed seam are all failures of this skill even if the code works.

This skill produces:

- **A working implementation of one ticket**, committed to that ticket's own branch, in that ticket's own worktree
- **Tests written before the code they verify**, at the seam the ticket declares, satisfying every acceptance criterion
- The ticket file with its acceptance criteria **checked off**
- A hand-off to `cfd-code-review`

It's also the skill that applies fixes when `cfd-code-review` sends issues back, then re-triggers review — see **Revision mode** below.

## Required inputs

Read before starting:

- **The ticket file** (`tickets/<NN>-<slug>.md`) being implemented — its schema touchpoints, UI touchpoints, testing seam, coverage bar, and acceptance criteria are all binding.
- **`SCHEMA.md`** — column names and types must match exactly. Do not add, rename, or retype a column to make implementation easier; if the ticket can't be built against the schema as written, stop and send the user back to `cfd-schema`.
- **`UI-DECISIONS.md`** — the agreed variant (or combination) for any screen this ticket touches. Do not "improve" the UI beyond what was decided; if something genuinely doesn't work as agreed, stop and flag it rather than silently changing it.
- **`CONTEXT.md`** — naming in code (types, functions, variables that represent domain concepts) should use canonical terms, not synonyms.
- **`SPEC.md`** — for the business rules, validation, permissions, and error handling this ticket's acceptance criteria were derived from, if more context is needed than the ticket itself provides.

If the ticket's schema or UI touchpoints don't match what's currently in `SCHEMA.md` / `UI-DECISIONS.md` (e.g. an earlier step was reopened and these drifted), stop and reconcile before writing code.

## Picking a ticket

Work the **frontier** of `tickets/`: any ticket whose "Blocked by" tickets are all complete. Implement one ticket per run of this skill — don't pull the next ticket in until this one is committed and handed to review (this restriction is per-worktree, not global; see below). If the user hasn't specified which ticket, list the **entire** current frontier and ask — not just one suggestion — since any of them could be started in its own worktree right away, including in parallel with others.

## Every ticket gets its own worktree

This is not optional or parallel-only — do it even when only one ticket is in flight, because this instance of the skill has no way to know whether another ticket is about to be started elsewhere. Isolating every ticket by worktree from the start means there's never a moment where two tickets could collide on a shared branch.

- **Branch and worktree per ticket, before writing any code.** Create `ticket/<NN>-<slug>` off the integration branch (the branch tickets are implemented against — usually the feature/epic branch or `main`), and check it out into its own worktree directory (e.g. `../<repo>-ticket-<NN>/`):

  ```
  git worktree add ../<repo>-ticket-<NN> -b ticket/<NN>-<slug> <integration-branch>
  ```

  If a worktree for this ticket already exists (e.g. resuming after a break, or entering revision mode), reuse it rather than creating a new one.

- **Run this skill inside that worktree.** All reads (`SCHEMA.md`, `UI-DECISIONS.md`, `CONTEXT.md`, `SPEC.md`, the ticket file) and all commits happen inside the ticket's own worktree, on the ticket's own branch.
- **"The current branch" throughout this skill means the ticket's branch**, inside the ticket's worktree.
- **Ticket files don't conflict.** Each ticket only edits its own file under `tickets/`, so branches editing different ticket files merge cleanly. Don't edit another ticket's file from inside this one's worktree.
- **Staleness is the user's call, not this skill's.** If a ticket's worktree has been open a while and other tickets have merged into the integration branch in the meantime, this skill doesn't auto-rebase — flag it if the diff looks like it might conflict, but let the user decide when to sync (`git rebase <integration-branch>` or `git merge <integration-branch>` inside the worktree).
- **Hand off per-worktree.** Each ticket hands off to `cfd-code-review` independently once committed — review happens inside the same worktree, against that ticket's branch.

## Test-driven implementation

TDD here means the red → green loop, held to the seam the ticket already declares — this skill does not renegotiate the seam.

### What a good test is

Tests verify behavior through the ticket's declared seam, not implementation details. A good test reads like a specification — "user can cancel an order before it ships" — and survives refactors because it doesn't reach into internal structure.

### Seams — where tests go

A **seam** is the public boundary being tested at: the interface where behavior is observed without reaching inside (an API endpoint, a service function, a rendered component's interaction, a CLI command). Tests live at the seam the ticket names, never against internals. Do not pick a different or additional seam mid-implementation without stopping to confirm it with the user first — the seam was already agreed in `cfd-spec`.

### Anti-patterns to avoid

- **Implementation-coupled** — mocking internal collaborators, testing private methods, or verifying through a side channel (querying the database directly instead of the public interface). Tell: the test breaks on a refactor even though behavior didn't change.
- **Tautological** — the assertion recomputes the expected value the way the code does, so it passes by construction. Expected values must come from an independent source: a known-good literal, a worked example, the ticket's acceptance criteria, or `SPEC.md`.
- **Horizontal slicing** — writing all the tests first, then all the implementation. Work in **vertical slices**: one test → one minimal implementation → repeat. Each test is a tracer bullet that responds to what the last cycle taught you, not a pre-imagined shape of the whole ticket.

### Rules of the loop

- **Red before green.** Write the failing test first, then only enough code to pass it. Don't anticipate later acceptance criteria or add speculative handling for cases not yet under test.
- **One slice at a time.** One acceptance criterion, one test, one minimal implementation per cycle.
- **Refactoring happens after green, not inside the loop.** Once a slice is green, clean it up before moving to the next slice — but don't mix refactoring into the red→green cycle itself.

### Coverage bar

A ticket cannot be handed to `cfd-code-review` while any acceptance criterion lacks a passing test at the declared seam. That's the floor, not the target.

Beyond the floor, measure line/branch coverage for the code this ticket touches using whatever's idiomatic for the stack (e.g. `dotnet test --collect:"XPlat Code Coverage"` with coverlet in .NET, `nyc`/`vitest --coverage` in JS/TS, `coverage.py` in Python) — don't skip measuring just because no threshold is wired into the build. Target **90% line coverage on the code this ticket touches**, and don't let this ticket's changes lower the project's existing overall coverage if that's already being tracked.

If that target genuinely can't be reached for something in this ticket, do not silently ship at a lower number. Stop and tell the user explicitly: what's under-covered, the actual percentage achieved, and *why* it can't reasonably go higher (e.g. a thin adapter with no branching logic, a framework-generated file, a UI state that requires a browser to exercise and the project has no such test setup). The user decides whether that's acceptable for this ticket — it is never this skill's call to make quietly.

## Housekeeping while implementing

- **Typecheck regularly** — after each slice, not just at the end.
- **Run the single test file for the current slice regularly** — don't wait for the full suite to get feedback.
- **Run the full test suite once, at the end**, before committing, to catch cross-ticket regressions.
- **Stay inside the ticket's scope.** If implementation surfaces a need genuinely outside this ticket's acceptance criteria (a missing schema field, an unspecified edge case, a UI state nobody decided), stop and flag it to the user rather than deciding it unilaterally in code — route it back to the earlier step it belongs to, or capture it as a new ticket rather than quietly expanding this one.

## Handing a ticket to review

Implementing a ticket does not close it — only the user, acting on a `cfd-code-review` report, closes a ticket. This skill's job ends at a reviewable commit, not a finished one.

1. Confirm every acceptance criterion on the ticket is backed by a passing test at the declared seam, and that the coverage bar above is met or any shortfall has been explicitly surfaced to the user.
2. Update the ticket file: tick the completed checkboxes, and set its status to `in-review` (not `done` or `closed`).
3. Commit the work to the ticket's own branch, in its own worktree (see **Every ticket gets its own worktree** above). Reference the ticket number in the commit message.
4. Hand off to `cfd-code-review` for this ticket's diff. Do not start the next ticket in *this* worktree until this one has been accepted by the user or the user explicitly says to move on regardless — this restriction is per-worktree, not global: a different ticket running in its own worktree elsewhere is unaffected.

## Revision mode (after code review)

When `cfd-code-review` reports issues on a ticket and the user chooses to send them back rather than accept the code as-is:

1. Treat the review findings as the input — don't re-derive them, and don't re-open scope beyond what the findings raised.
2. Apply fixes using the same red→green discipline where the fix involves behavior: if a finding describes a missing or wrong behavior, write the failing test that exposes it before fixing it. Findings that are pure style/structure (no behavior change) don't need a new test, but shouldn't break existing ones.
3. Re-run typechecking and the full test suite.
4. Commit the fixes as a new commit referencing the ticket number and noting it addresses review feedback — do not amend the original commit.
5. Hand back to `cfd-code-review` to re-review. Repeat until the user accepts.

## Handoff

State plainly which ticket was implemented, that it's committed, and that it's now going to `cfd-code-review`. If in revision mode, state which review findings were addressed and that re-review is next.
