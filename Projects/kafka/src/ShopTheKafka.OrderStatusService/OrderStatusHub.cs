using Microsoft.AspNetCore.SignalR;

namespace ShopTheKafka.OrderStatusService;

/// <summary>
/// Broadcasts every <see cref="OrderStatus"/> change to every connected client, per SPEC.md's "Real-time updates"
/// decision - no per-client filtering, since there's no authentication.
/// </summary>
public sealed class OrderStatusHub : Hub;
