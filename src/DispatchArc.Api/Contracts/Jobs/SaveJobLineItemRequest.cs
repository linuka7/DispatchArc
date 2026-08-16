namespace DispatchArc.Api.Contracts.Jobs;

public sealed class SaveJobLineItemRequest
{
    public string Description { get; init; } = string.Empty;

    public decimal Quantity { get; init; }

    public decimal UnitPrice { get; init; }
}