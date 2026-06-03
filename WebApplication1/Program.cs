using JasperFx;
using JasperFx.Events;
using JasperFx.MultiTenancy;
using Marten;
using WebApplication1;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ApplyJasperFxExtensions();

var marten = builder.Services.AddMarten(o =>
{
    o.DatabaseSchemaName = "document_store";
    o.Events.StreamIdentity = StreamIdentity.AsString;
    o.Events.DatabaseSchemaName = "event_store";
    o.Events.AppendMode = EventAppendMode.Quick;
    o.Events.EnableAdvancedAsyncTracking = true;
    o.Events.EnableEventTypeIndex = true;
    o.Events.TenancyStyle = TenancyStyle.Conjoined;
    o.Events.AddEventType<BasketCreated>();
    o.DisableNpgsqlLogging = true;
    o.MultiTenantedWithSingleServer("PORT = 5432; HOST = 127.0.0.1; PASSWORD = 'Password12!'; USER ID = 'postgres';Persist Security Info=true;",
        x => x.WithTenants("1", "2", "3").InDatabaseNamed("tenant_group1").WithTenants("10", "11", "12").InDatabaseNamed("tenant_group2"));
    o.Advanced.DefaultTenantUsageEnabled = false;
});
marten.UseLightweightSessions();
marten.ApplyAllDatabaseChangesOnStartup();
marten.AssertDatabaseMatchesConfigurationOnStartup();

var app = builder.Build();

app.MapPost("/tenants/{tenantCode}/basket", async (string tenantCode, IDocumentStore store, CancellationToken ct) =>
{
    await using var session = store.LightweightSession(tenantCode);
    var id = Guid.CreateVersion7().ToString();
    var stream = await session.Events.FetchForExclusiveWriting<Basket>(Basket.StreamName(id), ct);
    var rand = new Random();
    await Task.Delay(rand.Next(50, 500), ct);
    stream.AppendMany(new BasketCreated(tenantCode, id, $"New Basket {id}"));
    await session.SaveChangesAsync(ct);
    return Results.Ok(id);
});


return await app.RunJasperFxCommands(args);