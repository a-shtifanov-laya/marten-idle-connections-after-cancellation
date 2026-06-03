namespace WebApplication1;

public record Basket
{
    public static string StreamName(string id) => $"basket/{id}";
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