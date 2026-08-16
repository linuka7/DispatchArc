using DispatchArc.Api.Contracts.Payments;
using DispatchArc.Application.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DispatchArc.Api.Controllers;

[ApiController]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[Authorize(Policy = "TenantAccess")]
[Route("api/tenants/{tenantId:guid}/invoices/{invoiceId:guid}/payments")]
public sealed class PaymentsController : ControllerBase
{
    private readonly PaymentService _paymentService;

    public PaymentsController(
        PaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    [Authorize(Policy = "FinanceAccess")]
    [ProducesResponseType(typeof(InvoicePaymentSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoicePaymentSummaryResponse>>
        GetSummary(
            Guid tenantId,
            Guid invoiceId,
            CancellationToken cancellationToken)
    {
        var summary =
            await _paymentService.GetSummaryAsync(
                tenantId,
                invoiceId,
                cancellationToken);

        return summary is null
            ? NotFound()
            : Ok(summary);
    }

    [HttpPost]
    [Authorize(Policy = "FinanceAccess")]
    [ProducesResponseType(typeof(InvoicePaymentSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InvoicePaymentSummaryResponse>>
        Record(
            Guid tenantId,
            Guid invoiceId,
            RecordPaymentRequest request,
            CancellationToken cancellationToken)
    {
        try
        {
            var summary =
                await _paymentService.RecordAsync(
                    tenantId,
                    invoiceId,
                    request.Amount,
                    request.Method,
                    request.Reference,
                    request.PaidAtUtc,
                    cancellationToken);

            return summary is null
                ? NotFound()
                : Ok(summary);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(
                new ProblemDetails
                {
                    Title = "Payment cannot be recorded",
                    Detail = exception.Message,
                    Status =
                        StatusCodes.Status409Conflict
                });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(
                new ProblemDetails
                {
                    Title = "Invalid payment request",
                    Detail = exception.Message,
                    Status =
                        StatusCodes.Status400BadRequest
                });
        }
    }
}