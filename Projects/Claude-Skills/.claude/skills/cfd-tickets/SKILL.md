---
name: cfd-tickets
description: Step 5 of the cfd workflow. Break SPEC.md into tracer-bullet tickets, each a vertical slice that declares its blocking edges, the SCHEMA.md and UI-DECISIONS.md elements it must tightly follow, and the TDD seam and coverage bar it must be built against. Produces one ticket file per ticket. Use after cfd-spec and before cfd-implement.
disable-model-invocation: true
---

## Purpose

`SPEC.md` describes the whole feature; this skill cuts it into tickets small enough to build one at a time, in dependency order, without losing the discipline the earlier steps established. Every ticket produced here must be traceable back to the schema, the UI decision, and the testing seam it implements — a ticket that doesn't name those isn't finished being written.

This skill produces:

- **One ticket file per ticket**, in dependency order, each declaring what it blocks and is blocked by
- Every ticket scoped as a **vertical slice** — schema, behavior, and UI together, never a single-layer slice
- Every ticket carrying an explicit **TDD requirement**: the seam it's tested at (from `SPEC.md`) and the coverage bar it must meet before it's done

Do not proceed to `cfd-implement` on any ticket until this skill has produced the full breakdown and the user has approved it.

## Required inputs

Read before starting:

- **`SPEC.md`** — the primary input. User stories, implementation decisions, testing decisions (seams), and out-of-scope items all come from here.
- **`PROJECT.md`**, **`CONTEXT.md`**, **`SCHEMA.md`**, **`UI-DECISIONS.md`** — fixed constraints. Ticket titles and descriptions use `CONTEXT.md` vocabulary; every ticket that touches data must map to real `SCHEMA.md` tables/columns; every ticket that touches UI must map to a decision in `UI-DECISIONS.md`. Do not invent schema fields or UI behavior not already agreed — if a ticket seems to need something the earlier documents don't cover, stop and send the user back to the relevant earlier step rather than deciding it here.

If `SPEC.md` is missing, stop and tell the user to run `cfd-spec` first.

## Process

### 1. Gather context

Read `SPEC.md` in full — every user story, every implementation decision, the confirmed testing seam(s), and everything marked out of scope. Cross-check it against `SCHEMA.md` and `UI-DECISIONS.md` so every ticket you draft can cite the exact table/column or screen/variant it implements.

### 2. Explore the codebase

If code already exists, explore it to understand current state. Look for opportunities to **prefactor** — restructure existing code first to make the real implementation easier. "Make the change easy, then make the easy change." Any prefactoring becomes its own ticket, sequenced first.

### 3. Draft vertical slices

Break the work into **tracer bullet** tickets:

- Each slice cuts a narrow but **complete** path through every layer it touches (schema usage, behavior, UI) — vertical, never a horizontal slice of a single layer.
- A completed slice is demoable or verifiable on its own.
- Each slice is sized to fit in a single fresh context window.
- Any prefactoring is sequenced first, ahead of the slices it unblocks.

Give each ticket its **blocking edges** — the other tickets that must complete before it can start. A ticket with no blockers can start immediately.

**Wide refactors are the exception to vertical slicing.** A wide refactor is one mechanical change — rename a column, retype a shared symbol — whose blast radius fans across the whole codebase, so a single edit breaks many call sites at once and no vertical slice can land green. Don't force it into a tracer bullet; sequence it as **expand → contract**: first expand (add the new form beside the old so nothing breaks), then migrate call sites in batches sized by blast radius (each batch its own ticket, blocked by the expand), finally contract (delete the old form once no caller remains, blocked by every migrate batch). If even the batches can't stay green alone, let them share an integration branch that all block a final integrate-and-verify ticket — green is promised only there.

### 4. Attach schema, UI, and testing requirements to every ticket

For every ticket, before presenting it to the user, fill in:

- **Schema touchpoints** — the exact `SCHEMA.md` table(s)/column(s) this ticket reads or writes. If the ticket needs something not in `SCHEMA.md`, stop and flag it rather than guessing.
- **UI touchpoints** — the exact `UI-DECISIONS.md` screen/flow/variant this ticket implements, if it has a UI component. If the ticket needs UI behavior not covered there, flag it the same way.
- **Testing seam** — the seam from `SPEC.md`'s Testing Decisions that this ticket's tests must be written against. A ticket cannot specify its own seam; it inherits the one already agreed.
- **Coverage bar** — every acceptance criterion on the ticket must map to at least one test at the agreed seam, written before the implementation that satisfies it (red → green). The ticket is not done while any acceptance criterion has no covering test, and it must not lower the project's existing coverage if a coverage tool is already configured.

### 5. Quiz the user

Present the proposed breakdown as a numbered list. For each ticket, show:

- **Title**
- **Blocked by**
- **What it delivers** — the end-to-end behavior this ticket makes work
- **Schema / UI touchpoints**

Ask the user:

- Does the granularity feel right (too coarse / too fine)?
- Are the blocking edges correct — does each ticket depend only on tickets that genuinely gate it?
- Should any tickets be merged or split further?

Iterate until the user approves the breakdown.

### 6. Publish the tickets

Write one file per ticket under `tickets/<NN>-<slug>.md`, numbered from `01` in dependency order (blockers first). Never write a single combined file. Use the template below.

Work the **frontier**: any ticket whose blockers are all done can be picked up next. For a purely linear chain that means top to bottom. Do not close or modify `SPEC.md` or any earlier-step document — tickets reference them, they don't rewrite them.

```md
# <NN> — <Ticket title>

**What to build:** the end-to-end behavior this ticket makes work, from the user's perspective — not a layer-by-layer implementation list.

**Blocked by:** the numbers/titles of the tickets that gate this one, or "None — can start immediately."

**Status:** ready-for-agent

## Schema touchpoints

- `SCHEMA.md` table(s)/column(s) this ticket reads or writes.

## UI touchpoints

- `UI-DECISIONS.md` screen/flow/variant this ticket implements (omit if this ticket has no UI component).

## Testing requirements

- **Seam:** the confirmed seam from `SPEC.md` this ticket's tests are written against.
- **Coverage bar:** every acceptance criterion below must have at least one test at that seam, written before the code that satisfies it.

## Acceptance criteria

- [ ] Acceptance criterion 1
- [ ] Acceptance criterion 2
```

Avoid specific file paths or code snippets in ticket bodies — they go stale fast. Exception: if a prototype produced a snippet that encodes a decision more precisely than prose can (state shape, schema fragment), inline it and note briefly that it came from a prototype.

## Handoff

State plainly that step 5 is complete, list the tickets written (in dependency order) and the directory they're in, and confirm the user approved the breakdown. Suggest the user moves on to the `cfd-implement` skill, working the frontier of unblocked tickets first, and treating each ticket's schema touchpoints, UI touchpoints, and testing seam as fixed unless the user reopens an earlier step.
