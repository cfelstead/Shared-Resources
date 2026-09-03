---
name: cfd-understand
description: Step 1 of the cfd workflow. Relentlessly interview the user to reach shared understanding of a problem, pin down domain terminology so there's no vocabulary drift later, and agree a technology stack. Produces PROJECT.md, CONTEXT.md, and ADRs. Use at the start of a new feature or project, before any schema, prototype, spec, or ticket work begins.
disable-model-invocation: true
---

## Purpose

Before any schema, prototyping, spec, or ticket work happens, the problem, the vocabulary used to describe it, and the technology stack must all be pinned down and agreed with the user. This skill runs that interview and produces the three artifacts the rest of the `cfd` pipeline depends on:

- **`PROJECT.md`** — the problem, the goals, and the agreed stack
- **`CONTEXT.md`** — the project's glossary, so every later step uses the same words for the same things
- **`docs/adr/NNNN-*.md`** — a record of *why* for any hard-to-reverse decision made along the way (the stack choice will almost always produce one)

Do not proceed to schema design, prototyping, spec writing, or ticket writing until this skill has ended and the user has confirmed shared understanding.

## The interview

Interview the user relentlessly until you reach a shared understanding of the problem, the domain vocabulary, and the technology stack. Map this as a **design tree**: every decision branches into the decisions that hang off it. Three branches are always present in this tree, in addition to whatever the problem itself surfaces:

1. **Problem branch** — what's being built, for whom, and why. What does success look like? What's explicitly out of scope?
2. **Domain terminology branch** — the nouns and verbs of the problem space, and exactly one canonical word for each concept.
3. **Technology stack branch** — data layer, backend framework, frontend framework, hosting/deploy target, and anything else load-bearing enough to be expensive to change later.

None of these three branches may be skipped, deferred, or left implicit. A session cannot end with the problem understood but the stack unpicked, or the stack picked but two competing terms still in play for the same concept.

### Working the tree in rounds

The **frontier** is every decision whose prerequisites are already settled — the questions you can ask *now* without guessing at answers you haven't heard yet. Ask the whole frontier in one round: number each question and give your recommended answer. Then wait for the user's answers before the next round.

Format each question like this:

```
❓ **Q1** - **<question title>**: <question body, might be multiple paragraphs, including multiple choices>

➡️ <your recommended answer>
```

Each round the user answers reshapes the tree — settled decisions push the frontier outward and unblock questions that depended on them. Recompute the frontier and ask the next round. A question whose answer depends on another question still open in this round belongs to a *later* round, not this one.

Finding *facts* is your job, never the user's. When a frontier question needs a fact from the environment (existing code, existing config, existing docs, package manifests, etc.), go find it yourself or dispatch a sub-agent to find it — don't ask the user for anything you could look up. Don't block the whole round on it: a running exploration is an unsettled prerequisite, so only the questions downstream of it wait — ask the rest of the frontier now. The *decisions* are the user's — put each to them and wait for an answer.

The session is done when the frontier is empty across all three branches: the problem is understood, every domain term has exactly one agreed meaning, and the stack is fully picked. Nothing may be silently assumed. Do not write the final artifacts, and do not tell the user the step is complete, until they've explicitly confirmed shared understanding.

## Domain terminology discipline

Run this discipline continuously during the interview, not as a separate pass afterwards — capture and correct vocabulary the moment it comes up.

- **Challenge conflicting terms immediately.** If the user uses a term that conflicts with something already agreed in this session (or already recorded in an existing `CONTEXT.md`), stop and call it out: "Earlier we agreed 'X' means A — you just used it to mean B. Which is it?"
- **Sharpen fuzzy or overloaded language.** If the user uses a vague or overloaded term, propose a precise canonical term and confirm it. "You're saying 'account' — do you mean the Customer or the User? Those need to be different words."
- **Force precision with concrete scenarios.** When a relationship between two concepts is being discussed, invent a specific edge-case scenario and ask the user to place it. This is how fuzzy boundaries get found.
- **Cross-reference against any existing code.** If a codebase already exists, check whether it agrees with what the user is describing. Surface contradictions rather than silently trusting either source.
- **One concept, one word.** For every resolved term, there must be exactly one canonical word. Every synonym the user or codebase used for the same concept gets recorded as something to *avoid*, not as an alternative.
- **Record the moment a term is resolved**, not batched at the end — add it to the running `CONTEXT.md` draft as soon as it's settled, so a mid-session review of the file always reflects the current state of agreement.

`CONTEXT.md` is a glossary and nothing else — it must stay totally devoid of implementation details. Do not use it as a spec, a scratchpad, or a place to record architectural or technology decisions; those go in `PROJECT.md` and ADRs respectively.

## Technology stack branch

Treat the stack as a first-class part of the design tree, not something that only comes up if the conversation happens to touch it. At minimum, settle:

- **Data layer** — database/storage technology, and how it's hosted
- **Backend** — language/framework
- **Frontend** — language/framework (if applicable)
- **Hosting / deployment target**
- Anything else specific to this project that would take real effort to change later (auth provider, message bus, third-party APIs the architecture commits to, etc.)

For each, ask what the user wants, offer a recommendation if they have no preference, and record the answer plus a one-line reason. Do not infer a stack choice from an existing codebase without confirming it with the user — an existing choice still needs to be an explicit, agreed decision for this project, not an assumption.

## Recording decisions as ADRs

Offer to record an Architecture Decision Record whenever a decision made during this session is:

1. **Hard to reverse** — the cost of changing your mind later is meaningful
2. **Surprising without context** — a future reader will look at the outcome and wonder "why did they do it this way?"
3. **The result of a real trade-off** — there were genuine alternatives and one was picked for specific reasons

If any of the three is missing, skip the ADR — don't record decisions that were obvious or costless to make.

**The technology stack decision almost always qualifies** — treat it as the default case, not something to notice only if it happens to fit the rubric. Other candidates that come up during a problem-understanding session: an explicit scope exclusion that a reasonable reader would assume was in scope, a deliberate rejection of an obvious alternative, or a hard external constraint (compliance, an existing partner API contract, etc.).

### ADR format

Store ADRs at `docs/adr/NNNN-slug.md`, numbered sequentially — scan the directory for the highest existing number and increment by one. Create `docs/adr/` lazily, only when the first ADR is needed.

```md
# {Short title of the decision}

{1-3 sentences: what's the context, what did we decide, and why.}
```

That's the whole template — an ADR can be a single paragraph. Only add these sections when they add genuine value, which is uncommon:

- **Status** frontmatter (`proposed | accepted | deprecated | superseded by ADR-NNNN`) — only if the decision is likely to be revisited
- **Considered Options** — only when the rejected alternatives are worth remembering
- **Consequences** — only when non-obvious downstream effects need calling out

## Closing the session: writing the artifacts

Once the frontier is empty on all three branches and the user has explicitly confirmed shared understanding, write (or update) the following. Create each file lazily — only once it has real content.

### `CONTEXT.md` (project glossary)

One file at the repo root for a single-context project:

```md
# {Context Name}

{One or two sentence description of what this context is and why it exists.}

## Language

**Order**:
{A one or two sentence description of the term}
_Avoid_: Purchase, transaction

**Customer**:
A person or organization that places orders.
_Avoid_: Client, buyer, account
```

Rules:

- **Be opinionated.** One best word per concept; every synonym goes under `_Avoid_`.
- **Keep definitions tight** — one or two sentences, defining what a thing IS, not what it does.
- **Only include terms specific to this project.** General programming concepts don't belong, even heavily-used ones.
- **Group under subheadings** if natural clusters emerge; otherwise a flat list is fine.

If the project genuinely spans multiple bounded contexts, use a root `CONTEXT-MAP.md` instead, listing each context, where it lives, and how the contexts relate:

```md
# Context Map

## Contexts

- [Ordering](./src/ordering/CONTEXT.md) — receives and tracks customer orders
- [Billing](./src/billing/CONTEXT.md) — generates invoices and processes payments

## Relationships

- **Ordering → Billing**: Ordering emits `OrderPlaced` events; Billing consumes them to generate invoices
```

Default to a single `CONTEXT.md` unless the problem branch of the interview surfaced genuinely separate bounded contexts — don't create the multi-context structure speculatively.

### `PROJECT.md` (project summary)

One file at the repo root, written once the interview is complete:

```md
# {Project name}

## Problem

{The problem from the user's perspective — what's broken or missing today.}

## Goals

- {Goal 1}
- {Goal 2}

## Non-goals / out of scope

- {Explicitly excluded thing 1}

## Technology stack

| Layer | Choice | Why |
|---|---|---|
| Data | {choice} | {one-line reason, link to ADR if one exists} |
| Backend | {choice} | {one-line reason} |
| Frontend | {choice} | {one-line reason} |
| Hosting | {choice} | {one-line reason} |

## References

- Domain glossary: [CONTEXT.md](./CONTEXT.md)
- ADRs: [docs/adr/](./docs/adr/)
```

`PROJECT.md` holds the summary and the stack decision; it never duplicates glossary definitions from `CONTEXT.md` — link to them instead.

## Handoff

State plainly to the user that step 1 is complete, name the files written, and that the next step is schema design — which must treat `CONTEXT.md`'s terms as fixed vocabulary and `PROJECT.md`'s stack as a fixed constraint unless the user reopens this step. Suggest the user moves on to the `cfd-schema` skill.
