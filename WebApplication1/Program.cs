using JasperFx;
using JasperFx.Events;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using JasperFx.MultiTenancy;
using Marten;
using Marten.Storage;
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
    o.Events.UseOptimizedProjectionRebuilds = true;
    o.Events.AddEventType<BasketCreated>();
    o.DisableNpgsqlLogging = true;
    o.MultiTenantedWithSingleServer("PORT = 5432; HOST = 127.0.0.1; PASSWORD = 'Password12!'; USER ID = 'postgres';Persist Security Info=true; Connection Idle Lifetime=30; Connection Pruning Interval=10;",
        x => x.WithTenants("1", "2", "3").InDatabaseNamed("tenant_group1").WithTenants("10", "11", "12").InDatabaseNamed("tenant_group2"));
    o.Advanced.DefaultTenantUsageEnabled = false;
    o.Projections.LiveStreamAggregation<Basket>();
    o.Projections.DaemonLockId = 1;
    o.Projections.StaleSequenceThreshold = TimeSpan.FromSeconds(10);
    o.Projections.Add<BasketProjection>(ProjectionLifecycle.Async);
    o.RegisterDocumentType<BasketDocument>();
    o.Policies.AllDocumentsAreMultiTenanted();
});
marten.UseLightweightSessions();
marten.ApplyAllDatabaseChangesOnStartup();
marten.AssertDatabaseMatchesConfigurationOnStartup();
marten.AddAsyncDaemon(DaemonMode.HotCold);

var app = builder.Build();

app.MapPost("/tenants/{tenantCode}/basket", async (string tenantCode, IDocumentStore store, CancellationToken ct) =>
{
    await using var session = store.LightweightSession(tenantCode);
    var id = Guid.CreateVersion7().ToString();
    
    var rand = new Random();
    var temp1 = session.Events.FetchLatest<Basket>(Basket.StreamName(id), ct);
    await Task.Delay(rand.Next(50, 300), ct);
    var temp2 = session.Query<BasketDocument>().Where(x => x.Id == id).FirstOrDefaultAsync(ct);
    
    var stream = await session.Events.FetchForExclusiveWriting<Basket>(Basket.StreamName(id), ct);
    await Task.Delay(rand.Next(50, 300), ct);
    stream.AppendMany(new BasketCreated(tenantCode, id, $"New Basket {id}"));
    await session.SaveChangesAsync(ct);
    return Results.Ok(id);
});


return await app.RunJasperFxCommands(args);