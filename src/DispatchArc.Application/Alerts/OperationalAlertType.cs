namespace DispatchArc.Application.Alerts;

public enum OperationalAlertType
{
    ApprovedJobNeedsScheduling = 1,
    ScheduledJobStartingSoon = 2,
    ScheduledJobOverdueStart = 3,
    CompletedJobNeedsInvoice = 4,
    InvoiceDueSoon = 5,
    InvoiceOverdue = 6
}