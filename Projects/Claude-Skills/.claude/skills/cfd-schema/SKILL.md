---
name: cfd-schema
description: Step 2 of the cfd workflow. Pin down the data storage schema — table names, columns, types, keys, and relationships — field by field, until the user understands and can support every part of it. Produces SCHEMA.md. Use after cfd-understand and before prototyping, spec, or ticket work.
disable-model-invocation: true
---

## Purpose

The schema is the part of the system the user must be able to support directly — when something breaks in production, they're the one looking at the data. This skill exists to make sure no column, type, or relationship enters the schema without the user having had it explained to them and having explicitly agreed to it. Speed is not the goal here; the user's comprehension is.

This skill produces:

- **`SCHEMA.md`** — the canonical, human-readable description of every table, column, key, and relationship, in domain vocabulary
- Updates to **`CONTEXT.md`** if schema design surfaces a term that wasn't pinned down in step 1
- **ADRs** for hard-to-reverse schema decisions (see below)

Do not proceed to prototyping, spec, or ticket work until this skill has ended and the user has confirmed they understand and agree with every table.

## Required inputs

Read before starting:

- **`PROJECT.md`** — for the agreed data layer (Postgres, SQLite, Mongo, etc.) and any constraints that bear on schema (compliance, scale, hosting). Do not re-litigate the stack choice here; if the data layer choice turns out to be wrong for the schema being designed, stop and send the user back to `cfd-understand` rather than quietly deciding differently.
- **`CONTEXT.md`** (and `CONTEXT-MAP.md` if present) — for the canonical domain vocabulary. Every table and column name must use a term already defined there. If a needed concept has no term yet, resolve it against the same discipline `cfd-understand` used (one canonical word, synonyms recorded as `_Avoid_`) and add it to `CONTEXT.md` before using it in the schema — don't invent schema-only vocabulary that diverges from the glossary.

If either file is missing, stop and tell the user to run `cfd-understand` first.

## The interview

Interview the user relentlessly until every table is settled. Map this as a **design tree**, same mechanics as `cfd-understand`:

The **frontier** is every decision whose prerequisites are already settled. Ask the whole frontier in one round: number each question, give a recommended answer, then wait.

```
❓ **Q1** - **<question title>**: <question body>

➡️ <your recommended answer>
```

Recompute the frontier after each round of answers. A question that depends on another still-open question belongs to a later round.

Finding *facts* is your job. If a codebase or existing database already exists, inspect it (or dispatch a sub-agent to) rather than asking the user to describe it from memory. Only the *decisions* go to the user.

### Design tree branches

1. **Entity branch** — walk every noun in `CONTEXT.md` that represents something the system needs to store. For each, decide: does it get its own table, is it embedded in a parent table, or is it a fixed lookup/enum? Don't silently promote every glossary term to a table — some are value objects or enums, not entities.
2. **Column branch, per table** — for every column: name (must be a `CONTEXT.md` term or an unambiguous attribute of one), type, nullable or not, default, and a one-line purpose. A column with no clear purpose the user can state back doesn't belong yet.
3. **Key and relationship branch** — primary key per table, foreign keys, cardinality of every relationship (one-to-one, one-to-many, many-to-many, and any join tables that implies), and what happens on delete (cascade / restrict / nullify) for every foreign key.
4. **Cross-cutting conventions branch** — settle these once, and apply them consistently to every table rather than re-deciding per table:
   - Primary key strategy (auto-increment / UUID / ULID / etc.)
   - Timestamp convention (`created_at` / `updated_at`, timezone handling)
   - Soft delete vs hard delete
   - Naming convention (snake_case, plural vs singular table names, etc.) — should match whatever the stack/ORM idiomatically expects
   - Multi-tenancy strategy, if applicable

None of these four branches may be skipped. The interview isn't done just because table names exist — every column in every table needs its type, nullability, default, and purpose settled.

### Walking the user through comprehension, not just approval

For every table, before moving on: state the table's purpose, then read back every column in plain language — what it holds, why it exists, what would go wrong if it were missing or the wrong type — and get an explicit confirmation from the user, not a silent nod. If the user can't explain a column back to you in their own words, treat that column as unresolved, not agreed. This is slower than presenting a finished schema for a single sign-off, and that's deliberate — a schema the user rubber-stamped without understanding fails the purpose of this skill even if it's technically correct.

The session is done when: the frontier is empty across all four branches, every table has been walked through and confirmed individually, and the user has explicitly confirmed they understand and support the whole schema. Do not write final artifacts, and do not tell the user the step is complete, before that confirmation.

## Recording decisions as ADRs

Offer an ADR whenever a schema decision is hard to reverse, would surprise a future reader, and reflects a real trade-off — same rubric as `cfd-understand`:

1. **Hard to reverse**
2. **Surprising without context**
3. **The result of a real trade-off**

Typical schema-level candidates: normalization vs. deliberate denormalization, the primary key strategy, a soft-delete policy, a multi-tenancy approach, or storing something as JSON/unstructured instead of modeling it relationally. Store ADRs at `docs/adr/NNNN-slug.md`, numbered sequentially, same template as `cfd-understand`:

```md
# {Short title of the decision}

{1-3 sentences: what's the context, what did we decide, and why.}
```

## Writing the artifacts

### `SCHEMA.md` (canonical schema documentation)

One file at the repo root (or `docs/SCHEMA.md` if the project keeps docs under `docs/`), one section per table:

```md
# Schema

{One or two sentences: what this schema stores and the data layer it targets, per PROJECT.md.}

## {TableName}

{One or two sentences: what this table represents and why it exists.}

| Column | Type | Nullable | Default | Purpose |
|---|---|---|---|---|
| id | uuid | no | generated | Primary key |
| {column} | {type} | {yes/no} | {default or —} | {one-line purpose} |

**Primary key:** {column(s)}
**Foreign keys:** {column → OtherTable.column, on delete: cascade/restrict/nullify}
**Indexes:** {list, or "none"}

---

## {NextTable}
...

## Relationships

{A short relationship list or a mermaid ER diagram — table → table, cardinality, and what the relationship means in domain terms.}
```

Every column name in `SCHEMA.md` must match a term in `CONTEXT.md` — if it doesn't, that's a bug in this skill's output, not an acceptable gap.

## Handoff

State plainly that step 2 is complete, name the files written or updated (`SCHEMA.md`, any `CONTEXT.md` additions, any ADRs), and confirm the user has said they understand and support the schema. Suggest the user moves on to the `cfd-prototype` skill (UI prototyping), which must treat this schema as a fixed constraint unless the user reopens this step.
