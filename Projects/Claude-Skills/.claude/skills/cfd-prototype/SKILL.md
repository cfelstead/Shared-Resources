---
name: cfd-prototype
description: Step 3 of the cfd workflow. Build several radically different throwaway UI variations for a screen or flow, let the user flip between them in the browser, and get an explicit agreed decision on the UI before any spec or ticket work begins. Use after cfd-schema and before cfd-spec.
disable-model-invocation: true
---

## Purpose

Before a spec or tickets get written, the UI needs to be something the user has actually seen and chosen — not something inferred from a description. This skill generates real, running, radically different UI variants for the screen or flow in question, lets the user flip between them live, and captures which one (or which parts of which ones) they picked. The output is an agreed UI decision, not a finished implementation.

This skill produces:

- **Running throwaway UI variants** the user can flip between in the browser
- **A captured decision** — which variant won, which pieces of others were kept, and why
- **`UI-DECISIONS.md`** — the durable record of that decision, in domain vocabulary, for `cfd-spec` to build on
- A **throwaway branch** holding the full set of variants as a primary source, once the winner is folded in

Do not proceed to `cfd-spec` or ticket work until this skill has ended and the user has explicitly picked a UI direction.

## Required inputs

Read before starting:

- **`PROJECT.md`** — for the stack (framework, styling system, component library) the variants must be built with.
- **`CONTEXT.md`** (and `CONTEXT-MAP.md` if present) — for domain vocabulary. Labels, copy, and component names in the variants should use canonical terms, not synonyms.
- **`SCHEMA.md`** — for what data actually exists to render. Variants must only display fields that exist in the schema, and must not imply data or operations the schema doesn't support. If a variant needs a field the schema doesn't have, stop and send the user back to `cfd-schema` rather than quietly inventing data.

If any of these three files is missing, stop and tell the user which earlier skill (`cfd-understand` or `cfd-schema`) to run first.

## Process

### 1. State the question and pick the variant count

Confirm with the user which screen or flow is being prototyped, and state the plan in one line before writing anything:

> "Three variants of the settings page, switchable via `?variant=`, on the existing `/settings` route."

Default to **3 variants**. More than 5 stops being radically different and starts being noise — cap there. If the user wants fewer because the direction is already narrow, that's fine — say so rather than padding to 3.

### 2. Pick where the variants live

A UI prototype is far easier to judge **butting up against the rest of the app** — real header, real navigation, real data shaped by `SCHEMA.md`, real density. A variant floating in a vacuum looks fine no matter what it is.

- **Preferred: adjustment to an existing page.** If the route already exists, render variants **on that same route**, gated by a `?variant=` URL search param. Keep existing data fetching, params, and auth as they are — only the rendered subtree changes per variant. If the thing being prototyped doesn't have a page yet but naturally belongs inside one (a new dashboard section, a new settings card, a new step in an existing flow), that still counts as this case — mount the variants inside the host page.
- **Last resort: a new throwaway page.** Only when there's genuinely no existing page to host the variants — an entirely new top-level surface, or a flow that can't be embedded anywhere sensible. Follow whatever routing convention the project already uses; don't invent a new top-level structure. Name the route so it's obviously a prototype (include `prototype` in the path or filename). Before committing to this, sanity-check that there's really no page to embed in — an empty route hides design problems a populated one would expose.

### 3. Generate radically different variants

Draft each variant against:

- The screen's purpose and the data available to it, per `SCHEMA.md`.
- The project's actual component library / styling system — whatever `PROJECT.md` and the existing codebase already use.
- A clear exported component name (`VariantA`, `VariantB`, `VariantC`).

Variants must be **structurally different** — different layout, different information hierarchy, different primary affordance — not just different colours or copy. Three slightly-tweaked card grids is not a set of variants. If two drafts come out too similar, redo one under an explicit constraint that rules out the shared pattern (e.g. "no card grid").

### 4. Wire them together with a floating switcher

Build a single switcher component, shared across both the existing-page and new-page cases:

```tsx
// pseudo-code — adapt to the project's framework
const variant = searchParams.get('variant') ?? 'A';
return (
  <>
    {variant === 'A' && <VariantA {...data} />}
    {variant === 'B' && <VariantB {...data} />}
    {variant === 'C' && <VariantC {...data} />}
    <PrototypeSwitcher variants={['A', 'B', 'C']} current={variant} />
  </>
);
```

The switcher is a small fixed-position bar at the bottom-centre of the screen:

- **Left/right arrows** cycle through variants (wraps around), and update the URL search param via the framework's router so the variant is shareable and reload-stable.
- **Variant label** shows the current key and, if the variant exports a name, that name too (e.g. `B — Sidebar layout`).
- **Keyboard** `←`/`→` also cycle, except when an `<input>`, `<textarea>`, or `[contenteditable]` is focused.
- Visually distinct from the page itself (high-contrast pill, subtle shadow) so it's obviously tooling, not part of what's being judged.
- Gated out of production builds (`process.env.NODE_ENV !== 'production'` or equivalent) so a stray merge can't ship it to real users.

### 5. Hand it to the user

Surface the URL and the variant keys. Ask the user to open it and flip through. The useful feedback is usually **"I want the header from B with the sidebar from C"** — treat that as the real answer, not a failure to pick cleanly.

### 6. Capture the decision explicitly

Do not infer a winner from a passing comment — get an explicit statement from the user of which variant (or which combination of parts) they've chosen, and write it into `UI-DECISIONS.md`:

```md
# UI Decisions

## {Screen or flow name}

**Decided:** {variant, or the specific combination chosen}
**Why:** {the user's stated reasoning}
**Prototype branch:** {throwaway branch name, for reference}

{Optionally: a short description or screenshot reference of the chosen layout, in domain vocabulary — what's shown, in what hierarchy, using which SCHEMA.md fields.}

---

## {Next screen or flow}
...
```

This file accumulates one section per screen/flow prototyped over the life of the project — don't overwrite prior sections.

### 7. Fold in the winner and clean up

- Fold the winning variant into the real page (existing-page case) or promote it to a real route (new-page case).
- Drop the losing variants and the switcher from the main branch — they don't belong in production code.
- Commit the **full set of variants**, including the losers, to a throwaway branch out of `main` — this is the primary source for the decision, not disposable scratch work. Reference that branch name in `UI-DECISIONS.md`.
- The folded-in winner still needs to be treated as prototype-quality code needing a rewrite during real implementation (`cfd-implement`) — no tests, minimal error handling, and it hasn't gone through `cfd-spec` or `cfd-tickets` yet. Don't let "it's already in the branch" substitute for the spec/ticket/TDD process that follows.

## Anti-patterns

- **Variants that differ only in colour or copy.** That's a tweak, not a prototype — real variants disagree about structure.
- **Sharing too much code between variants.** A shared `<Header>` is fine; a shared `<Layout>` defeats the point.
- **Wiring variants to real mutations.** Read-only prototypes are fine; if a variant needs to mutate, stub it. The question is "what should this look like," not "does the backend work."
- **Displaying fields or flows the schema doesn't support.** If `SCHEMA.md` doesn't have it, the variant can't show it as if it does.
- **Promoting the prototype directly to production.** It was written under prototype constraints — rewrite it properly during implementation.
- **Treating a vague nod as a decision.** If the user hasn't stated which variant (or combination) they want, the session isn't done.

## Handoff

State plainly that step 3 is complete, name the screen/flow prototyped, the decision captured in `UI-DECISIONS.md`, and the throwaway branch holding the full variant set. Suggest the user moves on to the `cfd-spec` skill, which must treat `UI-DECISIONS.md` — alongside `PROJECT.md`, `CONTEXT.md`, and `SCHEMA.md` — as a fixed constraint unless the user reopens this step.
