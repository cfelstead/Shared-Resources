---
name: cfd-spec
description: Step 4 of the cfd workflow. Synthesize PROJECT.md, CONTEXT.md, SCHEMA.md, and UI-DECISIONS.md into a spec, filling the remaining gaps — business rules, validation, permissions, error handling, and testing seams — by grilling the user only on what those documents don't already answer. Produces SPEC.md. Use after cfd-prototype and before cfd-tickets.
disable-model-invocation: true
---

## Purpose

By this point, *what* the system is (`PROJECT.md`), *what it stores* (`SCHEMA.md`), and *what it looks like* (`UI-DECISIONS.md`) are all agreed. What's still missing is *how it behaves*: the business rules, validation, permissions, and error/edge-case handling that turn a schema and a screen into a working feature — plus the testing seams that `cfd-tickets` and `cfd-implement` need in order to build test-first with real coverage.

This skill does not re-interview the user on anything already settled. It synthesizes the four existing documents, identifies exactly what behavioral and testing questions remain unanswered by them, and grills the user on **only that gap**. Where the earlier documents already answer something, state it as settled and move on — don't ask again.

This skill produces:

- **`SPEC.md`** — the single synthesized spec that `cfd-tickets` slices into tickets
- Confirmed **testing seams** for the feature, agreed with the user before any ticket or test gets written
- Updates to **`CONTEXT.md`** if a business-rule discussion surfaces a term that wasn't pinned down earlier
- **ADRs** for hard-to-reverse behavioral decisions (see below)

Do not proceed to `cfd-tickets` until this skill has ended and the user has confirmed the spec.

## Required inputs

Read before starting:

- **`PROJECT.md`** — problem, goals, non-goals, and stack. The spec must not contradict any of these; if it seems to need to, stop and send the user back to `cfd-understand`.
- **`CONTEXT.md`** (and `CONTEXT-MAP.md` if present) — canonical domain vocabulary. Use it throughout the spec; if a new term is needed, resolve it with the same one-word discipline used in earlier steps and add it to `CONTEXT.md`.
- **`SCHEMA.md`** — the data the feature can read and write. Every behavior described in the spec must be expressible against this schema; if it isn't, stop and send the user back to `cfd-schema` rather than quietly inventing fields.
- **`UI-DECISIONS.md`** — the agreed screens and flows. The spec's user stories and behavior must match what was actually decided there, not a re-imagining of the UI.

If any of these four files is missing, stop and tell the user which earlier skill to run first.

## Process

### 1. Explore and identify the gap

Read the four input documents plus the current state of the codebase (if code already exists). From that, work out what's already decided versus what's genuinely open. The open questions are almost always in these categories:

- **Business rules** — what makes an action valid or invalid, beyond the schema's own constraints (e.g. "an order can only be cancelled before it ships," not just "orders have a status column").
- **Validation** — what input rules apply that the schema's types don't already enforce (format, range, cross-field checks).
- **Permissions / access control** — who can see or do what.
- **Error and edge-case handling** — what happens on the failure paths: empty states, conflicts, concurrent edits, partial failures.
- **Cross-cutting behavior implied but not decided by the UI** — e.g. `UI-DECISIONS.md` shows a delete button, but not whether it's a soft or hard delete, or what confirmation flow it needs.

Do not ask about anything the four documents already answer. If you're unsure whether something is settled, re-read the relevant document before asking the user — don't make them repeat themselves.

### 2. Grill the user on the gap only

Interview the user using the same design-tree/frontier mechanics as the earlier steps, scoped strictly to the gap identified in step 1:

The **frontier** is every open behavioral question whose prerequisites are already settled. Ask the whole frontier in one round: number each question, give a recommended answer, then wait.

```
❓ **Q1** - **<question title>**: <question body>

➡️ <your recommended answer>
```

Recompute the frontier after each round. A question that depends on another still-open question belongs to a later round. The session's behavioral portion is done when this frontier is empty — every business rule, validation, permission, and error path the feature touches has an explicit answer.

### 3. Sketch and confirm testing seams

Identify the public boundary(ies) at which this feature will be tested — the interface where behavior can be observed without reaching into internals. Prefer existing seams already used elsewhere in the codebase over inventing new ones; use the highest seam possible, and aim for as few seams as the feature honestly needs — one, if at all achievable.

State the proposed seam(s) to the user explicitly and get confirmation before writing them into the spec:

> "This feature will be tested at `<seam>` — is that the right boundary, or should something else be observable too?"

This is not optional and is not something to infer silently — `cfd-tickets` and `cfd-implement` will hold every test to whatever seam is written down here, per the TDD discipline: tests only at pre-agreed seams, verifying behavior through public interfaces, never implementation details.

### 4. Recording decisions as ADRs

Offer an ADR whenever a behavioral decision from step 2 is hard to reverse, would surprise a future reader, and reflects a real trade-off — same rubric as every earlier step:

1. **Hard to reverse**
2. **Surprising without context**
3. **The result of a real trade-off**

Typical candidates at this stage: a non-obvious permissions model, a deliberate choice not to handle a failure case (and why), or a business rule that overrides what the schema alone would suggest. Store ADRs at `docs/adr/NNNN-slug.md`, same template as earlier steps:

```md
# {Short title of the decision}

{1-3 sentences: what's the context, what did we decide, and why.}
```

### 5. Write `SPEC.md`

One file at the repo root (or alongside other specs if the project already has a convention for that), using the template below. Use `CONTEXT.md` vocabulary throughout. Do not include specific file paths or code snippets — they go stale fast — except a snippet from a prototype that encodes a decision more precisely than prose can (a state shape, a validation rule expressed as a type), trimmed to the decision-rich part only.

```md
# {Feature name}

## Problem Statement

The problem, from the user's perspective. Pull this from `PROJECT.md`; do not re-derive it from scratch.

## Solution

The solution, from the user's perspective, referencing the agreed schema (`SCHEMA.md`) and UI (`UI-DECISIONS.md`) rather than restating them.

## User Stories

A long, numbered list, covering the feature thoroughly:

1. As a {actor}, I want {feature}, so that {benefit}

## Implementation Decisions

Business rules, validation, permissions, and error/edge-case handling settled in this session — the behavioral layer on top of `SCHEMA.md` and `UI-DECISIONS.md`. Reference those documents rather than duplicating their content; only record what's *new* at this step.

## Testing Decisions

- The confirmed seam(s), and why they're the right boundary.
- What makes a good test here (external behavior only, not implementation details).
- Prior art — similar tests already in the codebase, if any.
- The coverage bar: every user story above should be traceable to at least one test at the confirmed seam.

## Out of Scope

What this spec deliberately excludes, including anything `PROJECT.md` already marked as a non-goal (referenced, not repeated) plus anything newly excluded at this step.

## Further Notes

Anything else worth recording that doesn't fit the sections above.
```

## Handoff

State plainly that step 4 is complete, name `SPEC.md` and any `CONTEXT.md` additions or ADRs written, and confirm the user has approved the spec — including the testing seams. Suggest the user moves on to the `cfd-tickets` skill, which must slice `SPEC.md` into tickets without reopening decisions already settled in `PROJECT.md`, `SCHEMA.md`, `UI-DECISIONS.md`, or this spec, unless the user explicitly reopens an earlier step.
