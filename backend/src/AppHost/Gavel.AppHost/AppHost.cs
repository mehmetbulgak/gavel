var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres").WithDataVolume("gavel-apphost-postgres");
var identityDb = postgres.AddDatabase("identitydb", "gavel_identity");
var auctionsDb = postgres.AddDatabase("auctionsdb", "gavel_auctions");
var notificationsDb = postgres.AddDatabase("notificationsdb", "gavel_notifications");
var redis = builder.AddRedis("redis");
var rabbitmq = builder.AddRabbitMQ("rabbitmq").WithManagementPlugin();

var mailpit = builder.AddContainer("mailpit", "axllent/mailpit", "v1.29")
    .WithHttpEndpoint(targetPort: 8025, name: "http")
    .WithEndpoint(targetPort: 1025, name: "smtp", scheme: "tcp");
var smtp = mailpit.GetEndpoint("smtp");

var identity = builder.AddProject<Projects.Gavel_Identity_Api>("identity")
    .WithReference(identityDb).WaitFor(identityDb)
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WithEnvironment("Database__MigrateOnStartup", "true");

var auctions = builder.AddProject<Projects.Gavel_Auctions_Api>("auctions")
    .WithReference(auctionsDb).WaitFor(auctionsDb)
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WithReference(redis).WaitFor(redis)
    .WithReference(identity).WaitFor(identity)
    .WithEnvironment("Database__MigrateOnStartup", "true")
    .WithEnvironment("Auth__RequireHttpsMetadata", "false");

var notifications = builder.AddProject<Projects.Gavel_Notifications_Api>("notifications")
    .WithReference(notificationsDb).WaitFor(notificationsDb)
    .WithReference(rabbitmq).WaitFor(rabbitmq)
    .WithReference(identity).WaitFor(identity)
    .WaitFor(mailpit)
    .WithEnvironment("Database__MigrateOnStartup", "true")
    .WithEnvironment("Auth__RequireHttpsMetadata", "false")
    .WithEnvironment("Email__Host", smtp.Property(EndpointProperty.Host))
    .WithEnvironment("Email__Port", smtp.Property(EndpointProperty.Port));

var gateway = builder.AddProject<Projects.Gavel_Gateway>("gateway")
    .WithReference(identity).WaitFor(identity)
    .WithReference(auctions).WaitFor(auctions)
    .WithReference(notifications).WaitFor(notifications)
    .WithEnvironment("Auth__RequireHttpsMetadata", "false");

builder.AddProject<Projects.Gavel_Simulator>("simulator")
    .WithReference(gateway).WaitFor(gateway)
    .WithEnvironment("Simulator__GatewayUrl", gateway.GetEndpoint("http"));

builder.AddViteApp("web", "../../../../frontend")
    .WithReference(gateway).WaitFor(gateway)
    .WithEnvironment("VITE_GATEWAY", gateway.GetEndpoint("http"))
    .WithExternalHttpEndpoints();

builder.Build().Run();
