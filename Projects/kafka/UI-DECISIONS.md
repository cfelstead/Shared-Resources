# UI Decisions

## Main pipeline screen (place an Order, watch OrderStatus live)

**Decided:** Variant C — Kanban board, with the Blazor default navigation/header removed.

**Why:** The user picked the kanban board over the sidebar+table (Variant A) and the vertical activity feed (Variant B). A column-per-`OrderStatus` board spatially shows the pipeline shape itself — an Order visibly moves from `Placed` through to `Shipped` (or a failure column), which sells "event-driven pipeline" better than a row in a table or a card in a feed. The user also explicitly asked for the default Blazor nav sidebar and top header to be removed so the board can use the full screen width — this app has exactly one screen, so a persistent nav added nothing.

**Prototype branch:** `prototype/main-screen-ui-variants` — holds the full 3-variant set (A: sidebar + table, B: activity feed, C: kanban board) plus the floating `?variant=` switcher used to compare them, as they stood before the winner was folded in.

**Layout, in domain vocabulary:** One column per `OrderStatus` value (`placed`, `paymentApproved`, `paymentFailed`, `inventoryReserved`, `inventoryBackorder`, `shipped`), in that order left to right. Each column header is tinted by status and shows a live count of Orders currently in it. Each card shows the Order's short id, item count, `totalAmount`, and — for `paymentFailed`/`inventoryBackorder` cards — the latest timeline entry's `detail` (e.g. "Card declined", "Widget unavailable"). An order-placement form sits in a full-width bar above the board, using `Item` name/quantity lines. No navigation chrome — the board is the entire screen.

---
