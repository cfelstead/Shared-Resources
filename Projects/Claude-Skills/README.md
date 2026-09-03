# cfd — a Claude Code skill pipeline for software development

`cfd` ("cfd" — a personal initialism, kept short by design) is a set of seven [Claude Code](https://claude.com/claude-code) skills that walk a software feature or project from a vague idea through to a reviewed, merge-ready pull request. Each skill is one stage of the pipeline, invoked explicitly (`/cfd-understand`, `/cfd-schema`, ...), and each stage writes a durable markdown artifact that every later stage treats as a fixed constraint.

It was built by adapting ideas from [Matt Pocock's `mattpocock-skills` plugin](https://github.com/mattpocock) to a specific, opinionated development process — one where the human stays deeply involved in the schema and the UI, TDD is non-negotiable, and nothing gets merged without an explicit review-and-accept step. None of the seven skills here depend on Matt's plugin; each is fully self-contained.

## Why this exists

The stock Matt Pocock skills are general-purpose building blocks — grill a plan, sketch a spec, cut tickets, implement, review. `cfd` is that same shape bent around one specific rule: **the person running it must end up able to support what gets built.** That mainly shows up in two ways:

- **The database schema is agreed field-by-field**, not just handed over for a sign-off. If you can't explain a column back in your own words, it isn't agreed yet.
- **Every later step is held to what earlier steps decided.** Once the schema and UI are agreed, the spec, tickets, implementation, and review are all forbidden from quietly deviating from them — any drift has to be surfaced and sent back, never smoothed over in code.

## The seven steps

Each step is a Claude Code skill living under `.claude/skills/<name>/SKILL.md`. Steps read the artifacts of every step before them and produce their own.

| # | Skill | What it does | Produces |
|---|---|---|---|
| 1 | `cfd-understand` | Interviews you (design-tree / frontier style) until the problem, the domain vocabulary, and the technology stack are all pinned down and agreed. | `PROJECT.md`, `CONTEXT.md`, ADRs |
| 2 | `cfd-schema` | Pins down every table, column, type, key, and relationship — and won't move on until you can explain each field back. | `SCHEMA.md` |
| 3 | `cfd-prototype` | Builds several radically different throwaway UI variants for a screen, lets you flip between them live, and captures which one you picked. | `UI-DECISIONS.md`, a throwaway branch |
| 4 | `cfd-spec` | Synthesizes steps 1–3 and grills you *only* on what's still open — business rules, validation, permissions, error handling, and the testing seam. | `SPEC.md` |
| 5 | `cfd-tickets` | Slices the spec into small, dependency-ordered, vertical-slice tickets, each naming the exact schema/UI elements and testing seam it must follow. | `tickets/NN-slug.md` |
| 6 | `cfd-implement` | Implements one ticket at a time, strictly test-first (red → green), holding tightly to the schema and UI already agreed. Also applies review fixes. | Committed code + tests |
| 7 | `cfd-code-review` | Reviews a ticket's diff on three independent axes — **Standards**, **Spec**, **Fidelity** (schema/UI/seam/coverage) — and reports to you for an accept/send-back decision. | A review report; on acceptance, a closed ticket (and optionally a PR) |

The loop between 6 and 7 repeats until you accept: `cfd-code-review` never edits code, and `cfd-implement` never closes a ticket — only you, acting on a review report, do that.

### The artifact chain

```
PROJECT.md ──┐
CONTEXT.md ──┼──► SCHEMA.md ──► UI-DECISIONS.md ──► SPEC.md ──► tickets/*.md ──► (implement ⇄ review) ──► done
             │        (step 1)      (step 2)          (step 3)     (step 4)         (step 5)   (step 6)    (step 7)
             └── domain glossary, referenced by every step from here on
```

Every step refuses to invent something an earlier step should have decided — if a gap shows up (a missing schema field, an undecided UI state), the skill stops and sends you back to the right earlier step instead of quietly deciding it.

## Where it diverges from Matt Pocock's skills

For anyone familiar with the original plugin, the deliberate departures are:

- **No shared dependency on Matt's skills.** Each `cfd` skill inlines the mechanics it needs (the grilling/frontier interview loop, the TDD red-green discipline, the seam vocabulary, the ADR rubric, the Fowler smell baseline) rather than pointing at `/grilling`, `/tdd`, `/domain-modeling`, etc. This trades some duplication for the skills being readable and modifiable in isolation.
- **A dedicated schema step** (`cfd-schema`) that doesn't exist as its own thing in the original plugin, with a hard requirement that the schema is *understood*, not just approved.
- **UI prototyping only covers the UI branch** of Matt's `prototype` skill (not the logic/state-machine branch) — schema and business-logic decisions are handled by `cfd-schema` and `cfd-spec` instead.
- **`cfd-spec` is a hybrid**, not pure synthesis. Matt's `to-spec` explicitly avoids interviewing the user; `cfd-spec` still doesn't re-ask anything already settled, but *does* grill on the behavioral gap (business rules, validation, permissions, errors) that earlier steps don't cover, plus the testing seam.
- **No issue-tracker integration.** Tickets are always local markdown files under `tickets/` — no GitHub/Linear wiring, no separate tracker-setup skill.
- **A third review axis, Fidelity**, added to Matt's Standards/Spec pair — checking the diff against `SCHEMA.md`, `UI-DECISIONS.md`, and the ticket's declared testing seam/coverage, independently of code quality or spec conformance.
- **Explicit closing semantics**: `cfd-implement` marks a ticket `in-review`, never `done`; only `cfd-code-review`, on acceptance, closes it. This was a deliberate change during design — see the commentary in the numbered design docs below.
- **An unconditional coverage bar** (target: 90% on the code a ticket touches), measured with whatever's idiomatic for the stack, with a required explicit escalation to the user whenever it can't be hit — rather than a bar that only applies "if a coverage tool happens to be configured."

## Using it

Each skill is invoked explicitly as a slash command in Claude Code, in order:

```
/cfd-understand   → PROJECT.md, CONTEXT.md, ADRs
/cfd-schema       → SCHEMA.md
/cfd-prototype    → UI-DECISIONS.md
/cfd-spec         → SPEC.md
/cfd-tickets      → tickets/*.md
/cfd-implement    → implements one ticket, hands off to review
/cfd-code-review  → reviews the ticket; you accept or send back to /cfd-implement
```

Steps 6 and 7 repeat per ticket until every ticket in `tickets/` is closed.

They're marked `disable-model-invocation: true`, so Claude won't pick them up automatically from conversation — you drive the pipeline explicitly, one slash command at a time.

## Repo layout

```
.claude/skills/<name>/SKILL.md          — the seven skills, as installed
proposal-docs/00-cfd-development-review.md   — design notes: review of the Matt Pocock skills this was seeded from
proposal-docs/01…07-proposed-skill-*.md      — the design history: each skill's draft, as proposed and then revised
README.md                               — this file
```

The files under `proposal-docs/` are a record of how each skill was designed and reasoned about — including places the design changed after review (e.g. why `cfd-implement` doesn't close tickets, why the coverage bar is unconditional). They're kept for anyone who wants the reasoning, not just the result; they're not needed to use the skills.

A copy of these skills also lives in the personal Claude Code skills directory (`~/.claude/skills/`) outside this repo, so they're available in any project, not just this one.
