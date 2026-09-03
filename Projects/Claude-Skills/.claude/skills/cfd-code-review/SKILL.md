---
name: cfd-code-review
description: Step 7 of the cfd workflow. Review a ticket's diff along three axes in parallel — Standards (repo conventions and code smells), Spec (does it match SPEC.md and the ticket's acceptance criteria), and Fidelity (does it tightly follow SCHEMA.md and UI-DECISIONS.md, and was it genuinely built TDD-first at the declared seam with the required coverage). Reports findings for the user to accept or send back to cfd-implement. Use after cfd-implement hands off a ticket.
disable-model-invocation: true
---

## Purpose

This is the gate between "implemented" and "done." Nothing closes a ticket except the user, acting on this skill's report. The three axes exist because a ticket can pass some and fail others independently — clean, well-tested code that quietly drifted from the agreed schema is still a failure, and a faithful implementation that ignores repo conventions is still a failure, and either can happen without the other. Reporting them separately stops one axis from masking the other.

This skill produces:

- **A three-axis report** (Standards / Spec / Fidelity), presented to the user for a decision
- Nothing else gets written or committed by this skill — it reviews, it doesn't fix

The user, after reading the report, does one of:

- **Accept** — the ticket is closed (its status set to `done`), and if the project uses GitHub, this skill offers to help open a pull request.
- **Send back** — the findings go to `cfd-implement` in revision mode. Once fixes land, this skill runs again on the new commit(s). Repeat until accepted.

## Pinning the fixed point

The diff under review is the ticket's own commits, not the whole branch. Find them via the ticket number referenced in commit messages (`cfd-implement` requires this):

```
git log --grep="<ticket-number>" --oneline
```

Confirm the resulting commit range with the user if it's ambiguous (e.g. the ticket number appears in unrelated commits, or this is a second review pass after revision-mode fixes and only the new commits should be in scope). Capture the diff command once: `git diff <base>...HEAD` (three-dot, against the merge-base) for the confirmed range. Fail here — not inside three parallel sub-agents — if the range doesn't resolve or the diff is empty.

## Identifying the sources

- **Spec source**: the ticket file (`tickets/<NN>-<slug>.md`) for this ticket's specific acceptance criteria, schema touchpoints, UI touchpoints, and declared seam — plus `SPEC.md` for the broader behavioral decisions (business rules, validation, permissions, error handling) the ticket was sliced from.
- **Standards sources**: anything in the repo documenting how code should be written (`CODING_STANDARDS.md`, `CONTRIBUTING.md`, `.editorconfig`-adjacent docs, etc.), plus the smell baseline below, which always applies on top of whatever the repo documents.
- **Fidelity sources**: `SCHEMA.md`, `UI-DECISIONS.md`, and `CONTEXT.md` — the ticket's schema/UI touchpoints are checked against these directly, not just against the ticket's own description of them (the ticket could itself be stale if an earlier step was reopened after the ticket was written).

### The smell baseline (Standards axis)

A fixed set of Fowler code smells (*Refactoring*, ch. 3) that applies even when the repo documents nothing. Two rules bind it: a documented repo standard always overrides the baseline where the two conflict; and every smell here is a labelled heuristic, never a hard violation — skip anything tooling already enforces (formatters, linters, analyzers).

- **Mysterious Name** — a function, variable, or type whose name doesn't reveal what it does or holds. → rename it; if no honest name comes, the design's murky.
- **Duplicated Code** — the same logic shape appears in more than one hunk or file in the change. → extract the shared shape, call it from both.
- **Feature Envy** — a method that reaches into another object's data more than its own. → move the method onto the data it envies.
- **Data Clumps** — the same few fields or params keep travelling together. → bundle them into one type, pass that.
- **Primitive Obsession** — a primitive or string standing in for a domain concept that deserves its own type. → give the concept its own small type.
- **Repeated Switches** — the same `switch`/`if`-cascade on the same type recurs across the change. → replace with polymorphism, or one map both sites share.
- **Shotgun Surgery** — one logical change forces scattered edits across many files in the diff. → gather what changes together into one module.
- **Divergent Change** — one file or module is edited for several unrelated reasons. → split so each module changes for one reason.
- **Speculative Generality** — abstraction, parameters, or hooks added for needs the ticket doesn't have. → delete it; inline back until a real need shows.
- **Message Chains** — long `a.b().c().d()` navigation the caller shouldn't depend on. → hide the walk behind one method on the first object.
- **Middle Man** — a class or function that mostly just delegates onward. → cut it, call the real target direct.
- **Refused Bequest** — a subclass or implementer that ignores or overrides most of what it inherits. → drop the inheritance, use composition.

## Spawning the three sub-agents in parallel

Each sub-agent gets its own context so the axes don't pollute each other. All three receive the diff command and the commit list.

**Standards sub-agent** — additionally receives the standards-source files found above, plus the smell baseline pasted in full. Brief: "Report, per file/hunk where relevant: (a) every place the diff violates a documented standard — cite the standard (file + rule); (b) any baseline smell you spot — name it and quote the hunk. Distinguish hard violations (documented-standard breaches) from judgement calls (baseline smells are always judgement calls, and a documented repo standard overrides the baseline). Skip anything tooling enforces. Under 400 words."

**Spec sub-agent** — additionally receives the ticket file and the relevant sections of `SPEC.md`. Brief: "Report: (a) acceptance criteria or SPEC.md requirements that are missing or partially implemented; (b) behavior in the diff that wasn't asked for by the ticket (scope creep); (c) requirements that look implemented but where the implementation looks wrong. Quote the ticket/spec line for each finding. Under 400 words."

**Fidelity sub-agent** — additionally receives `SCHEMA.md`, `UI-DECISIONS.md`, `CONTEXT.md`, the ticket's declared seam, and its stated coverage bar (and any explicitly-surfaced shortfall). Brief: "Report: (a) any table/column used, added, renamed, or typed in the diff that doesn't exactly match SCHEMA.md; (b) any UI behavior that doesn't match the agreed variant in UI-DECISIONS.md; (c) any test in the diff written against something other than the declared seam (reaching into internals, mocking past the boundary, querying storage directly instead of the public interface); (d) any code identifier for a domain concept that doesn't match CONTEXT.md vocabulary; (e) whether the coverage bar was actually met by real tests — flag tautological tests (assertions that recompute the expected value the way the code does) or any acceptance criterion whose 'passing test' doesn't actually exercise the behavior it claims to. Quote the relevant SCHEMA.md/UI-DECISIONS.md/CONTEXT.md line for each finding. Under 400 words."

If `SPEC.md` or the ticket file can't be found, skip the Spec sub-agent and note it in the final report. Fidelity and Standards always run.

## Aggregating and presenting

Present the three reports under `## Standards`, `## Spec`, and `## Fidelity` headings, verbatim or lightly cleaned. Do not merge or rerank findings across axes — a Fidelity failure doesn't outrank a Spec failure or vice versa; they're independent gates. End with a one-line summary: total findings per axis, and the worst issue *within each axis*, if any.

Then ask the user directly what they want to do:

```
❓ **Decision** - **Accept or send back?**: {one-line summary of the state of the three axes}

➡️ <your recommendation, if you have one>
```

## On acceptance

1. Set the ticket's status to `done` in its ticket file.
2. If the project uses GitHub (a remote points at github.com, or the user says so), offer to open a pull request for this ticket's commits — do not push or open the PR without the user confirming, per normal git-safety practice.
3. Report the frontier of remaining tickets so the user can decide what's next.

## On send-back

`cfd-implement` cannot be launched by this skill or any other agent — it is reserved for explicit user invocation, so do not attempt to call or trigger it yourself. Instead, give the user a ready-to-run `/cfd-implement` prompt they can paste themselves: the ticket number, revision mode, the commit range this review covered, and the specific findings to address (quote them, don't make the user re-derive them from the report above). If the user only wants a subset of findings addressed, reflect that subset in the prompt, not the full report. Do not attempt to fix anything here — this skill reviews, it doesn't edit code.

## Handoff

State plainly which ticket was reviewed, the commit range covered, and the outcome — accepted and closed, or sent back for revision. If accepted, name the frontier of tickets now unblocked or otherwise ready to pick up next.
