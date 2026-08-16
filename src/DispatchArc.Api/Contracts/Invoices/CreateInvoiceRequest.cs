namespace DispatchArc.Api.Contracts.Invoices;

public sealed class CreateInvoiceRequest
{
    public DateTimeOffset? DueAtUtc { get; init; }
}