using DispatchArc.Domain.Entities;
using DispatchArc.Domain.Enums;
using Xunit;

namespace DispatchArc.UnitTests.Domain;

public sealed class ServiceJobTests
{
    [Fact]
    public void NewJob_ShouldHaveExpectedDefaults()
    {
        var job = CreateJob();

        Assert.NotEqual(Guid.Empty, job.Id);
        Assert.Equal(JobStatus.New, job.Status);
        Assert.Equal(JobPriority.Normal, job.Priority);
        Assert.Null(job.AssignedTechnicianId);
        Assert.Null(job.ScheduledStartUtc);
        Assert.Null(job.ScheduledEndUtc);
    }

    [Fact]
    public void Job_ShouldMoveThroughCompleteWorkflow()
    {
        var job = CreateJob();
        var startUtc = DateTimeOffset.UtcNow.AddHours(1);
        var endUtc = startUtc.AddHours(2);

        job.MarkQuoted();
        Assert.Equal(JobStatus.Quoted, job.Status);

        job.Approve();
        Assert.Equal(JobStatus.Approved, job.Status);

        job.Schedule(startUtc, endUtc);
        Assert.Equal(JobStatus.Scheduled, job.Status);

        job.Start();
        Assert.Equal(JobStatus.InProgress, job.Status);

        job.Complete();
        Assert.Equal(JobStatus.Completed, job.Status);

        job.MarkInvoiced();
        Assert.Equal(JobStatus.Invoiced, job.Status);
    }

    [Fact]
    public void Job_CannotBeCompletedBeforeItStarts()
    {
        var job = CreateJob();

        var exception = Assert.Throws<InvalidOperationException>(
            job.Complete);

        Assert.Contains(
            "cannot move",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(JobStatus.New, job.Status);
    }

    [Fact]
    public void Job_CannotBeScheduledBeforeApproval()
    {
        var job = CreateJob();
        var startUtc = DateTimeOffset.UtcNow.AddHours(1);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            job.Schedule(startUtc, startUtc.AddHours(2)));

        Assert.Contains(
            "approved",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(JobStatus.New, job.Status);
    }

    [Fact]
    public void Schedule_EndMustBeAfterStart()
    {
        var job = CreateJob();
        var startUtc = DateTimeOffset.UtcNow.AddHours(1);

        job.Approve();

        var exception = Assert.Throws<ArgumentException>(() =>
            job.Schedule(startUtc, startUtc.AddMinutes(-30)));

        Assert.Contains(
            "after the start",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(JobStatus.Approved, job.Status);
    }

    [Fact]
    public void CompletedJob_CannotBeCancelled()
    {
        var job = CreateJob();
        var startUtc = DateTimeOffset.UtcNow.AddHours(1);

        job.Approve();
        job.Schedule(startUtc, startUtc.AddHours(2));
        job.Start();
        job.Complete();

        Assert.Throws<InvalidOperationException>(job.Cancel);
        Assert.Equal(JobStatus.Completed, job.Status);
    }

    [Fact]
    public void CancelledJob_CannotBeAssignedToTechnician()
    {
        var job = CreateJob();

        job.Cancel();

        Assert.Throws<InvalidOperationException>(() =>
            job.AssignTechnician(Guid.NewGuid()));

        Assert.Equal(JobStatus.Cancelled, job.Status);
        Assert.Null(job.AssignedTechnicianId);
    }

    private static ServiceJob CreateJob()
    {
        return new ServiceJob(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "JOB-TEST-001",
            "Repair air-conditioner",
            "Unit is not cooling.",
            JobPriority.Normal);
    }
}
