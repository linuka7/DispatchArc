namespace DispatchArc.Application.Invoices;

public sealed record InvoiceLineItemResponse(
    Guid Id,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);