var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder.AddKafka("kafka")
    .WithKafkaUI();

var orderService = builder.AddProject<Projects.ShopTheKafka_OrderService>("orderservice")
    .WithReference(kafka)
    .WaitFor(kafka);

var paymentService = builder.AddProject<Projects.ShopTheKafka_PaymentService>("paymentservice")
    .WithReference(kafka)
    .WaitFor(kafka);

var inventoryService = builder.AddProject<Projects.ShopTheKafka_InventoryService>("inventoryservice")
    .WithReference(kafka)
    .WaitFor(kafka);

var shippingService = builder.AddProject<Projects.ShopTheKafka_ShippingService>("shippingservice")
    .WithReference(kafka)
    .WaitFor(kafka);

var notificationService = builder.AddProject<Projects.ShopTheKafka_NotificationService>("notificationservice")
    .WithReference(kafka)
    .WaitFor(kafka);

var orderStatusService = builder.AddProject<Projects.ShopTheKafka_OrderStatusService>("orderstatusservice")
    .WithReference(kafka)
    .WaitFor(kafka);

builder.AddProject<Projects.ShopTheKafka_OrderUI>("orderui")
    .WithReference(orderService)
    .WithReference(orderStatusService)
    .WaitFor(orderService)
    .WaitFor(orderStatusService)
    .WithExternalHttpEndpoints();

builder.Build().Run();
