using JasperFx.Events;
using Marten.Events.Aggregation;

namespace WebApplication1;

public record Basket
{
    public static string StreamName(string id) => $"basket/{id}";
    public string Id { get; private init; } = null!;
    public string BasketId { get; private init; } = null!;
    public string Name { get; private init; } = null!;

    public Basket Apply(object e)
    {
        var x = this;
        return e switch
        {
            BasketCreated c => x with { BasketId = c.BasketId, Name = c.Name },
            _ => x
        };
    }
}

public record BasketCreated(string Tenant, string BasketId, string Name);

public class BasketProjection : SingleStreamProjection<BasketDocument, string>
{
    public override BasketDocument? Evolve(BasketDocument? snapshot, string id, IEvent e)
    {
        return e.Data switch
        {
            BasketCreated x => snapshot ?? new BasketDocument { BasketName = x.Name, LatestEvent = e.Sequence },
            _ => snapshot
        };
    }
}

public class BasketDocument
{
    public string Id { get; set; } = null!;
    public string? BasketName { get; set; }
    public long LatestEvent { get; set; }
}