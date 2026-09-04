# 07 — Real Blazor UI wired to the live backend

**What to build:** A person can open the app, place an Order through a real form, and watch it appear on the kanban board and move between columns live as the actual pipeline processes it — replacing the throwaway prototype's fake in-memory feed with real calls to OrderService and OrderStatusService.

**Blocked by:** 01, 02, 03, 04, 05, 06 — every backend service must actually run and produce/consume real events for an Order to visibly move across the board.

**Status:** done

## Schema touchpoints

- `SCHEMA.md`: `OrderStatus` (read via `GET /orders/{id}` and the SignalR hub)

## UI touchpoints

- `UI-DECISIONS.md`: the agreed Variant C kanban board — one column per `OrderStatus` value, order-placement form in a full-width bar above the board, no navigation chrome.

## Testing requirements

- **Seam:** HTTP + SignalR seam (`SPEC.md` Testing Decisions) — automated tests verify the UI's calls to OrderService (`POST /orders`) and OrderStatusService (`GET /orders/{id}`, SignalR subscription) produce correct results at that seam.
- **Coverage bar:** every acceptance criterion below that describes a backend interaction has at least one test at this seam. `SPEC.md` does not define a UI-rendering test seam, so the criteria describing visual layout/movement are verified manually, not by an automated test — this is a known, agreed gap, not an oversight.

## Acceptance criteria

- [x] Submitting the order-placement form calls the real `POST /orders` with the entered Items and, on success, the UI has the new `orderId` available for the board to render
- [x] The board subscribes to OrderStatusService's SignalR hub and renders every Order it has received an `OrderStatus` for, grouped into the correct column by `currentStatus`
- [x] A newly placed Order appears on the board in the `placed` column without a page refresh
- [x] (Manual verification — no agreed seam) As an Order's `currentStatus` changes, its card visibly moves to the corresponding column live
- [x] (Manual verification — no agreed seam) The screen matches the agreed Variant C layout: kanban board, order form above it, no navigation sidebar or header
- [x] The throwaway prototype's fake in-memory feed (`FakeOrderFeed` and related prototype-only code) is removed or fully replaced — no fake data path remains in the shipped UI
