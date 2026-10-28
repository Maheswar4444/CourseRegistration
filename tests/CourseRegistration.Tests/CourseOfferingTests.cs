using CourseRegistration;
using Xunit;

namespace CourseRegistration.Tests;

public sealed class CourseOfferingTests
{
    [Fact]
    public void Enroll_UsesAvailableCapacity()
    {
        var offering = new CourseOffering(2, _ => true);

        Assert.Equal(EnrollmentResult.Enrolled, offering.Enroll("A"));
        Assert.Equal(EnrollmentResult.Enrolled, offering.Enroll("B"));
        Assert.Equal(new[] { "A", "B" }, offering.GetSnapshot().EnrolledLearnerIds);
    }

    [Fact]
    public void Enroll_WhenFull_AddsEligibleLearnerToWaitlist()
    {
        var offering = new CourseOffering(1, _ => true);

        offering.Enroll("A");
        Assert.Equal(EnrollmentResult.Waitlisted, offering.Enroll("B"));
        Assert.Equal(new[] { "B" }, offering.GetSnapshot().WaitlistedLearnerIds);
    }

    [Fact]
    public void DuplicateRequests_DoNotChangeEnrollmentOrWaitlistPosition()
    {
        var offering = new CourseOffering(1, _ => true);
        offering.Enroll("A");
        offering.Enroll("B");
        offering.Enroll("C");

        Assert.Equal(EnrollmentResult.AlreadyActive, offering.Enroll("A"));
        Assert.Equal(EnrollmentResult.AlreadyActive, offering.Enroll("B"));
        Assert.Equal(new[] { "A" }, offering.GetSnapshot().EnrolledLearnerIds);
        Assert.Equal(new[] { "B", "C" }, offering.GetSnapshot().WaitlistedLearnerIds);
    }

    [Fact]
    public async Task ConcurrentEnrollments_WithOneSeatRemaining_EnrollsExactlyOneLearner()
    {
        var offering = new CourseOffering(2, _ => true);
        Assert.Equal(EnrollmentResult.Enrolled, offering.Enroll("Existing"));

        using var startBarrier = new Barrier(3);
        Task<EnrollmentResult> EnrollAfterBarrier(string learnerId) => Task.Run(() =>
        {
            if (!startBarrier.SignalAndWait(TimeSpan.FromSeconds(10)))
            {
                throw new TimeoutException("Enrollment task did not reach the start barrier in time.");
            }

            return offering.Enroll(learnerId);
        });

        var firstTask = EnrollAfterBarrier("First");
        var secondTask = EnrollAfterBarrier("Second");

        Assert.True(startBarrier.SignalAndWait(TimeSpan.FromSeconds(10)), "Enrollment tasks did not reach the start barrier in time.");
        var results = await Task.WhenAll(firstTask, secondTask);
        var snapshot = offering.GetSnapshot();

        Assert.Equal(1, results.Count(result => result == EnrollmentResult.Enrolled));
        Assert.Equal(1, results.Count(result => result == EnrollmentResult.Waitlisted));
        Assert.Equal(2, snapshot.EnrolledLearnerIds.Count);
        Assert.Equal(2, snapshot.Capacity);
        Assert.Single(snapshot.WaitlistedLearnerIds);
        Assert.Contains(snapshot.WaitlistedLearnerIds[0], new[] { "First", "Second" });
        Assert.Contains("Existing", snapshot.EnrolledLearnerIds);
        Assert.Single(snapshot.EnrolledLearnerIds.Intersect(new[] { "First", "Second" }));
        Assert.Empty(snapshot.EnrolledLearnerIds.Intersect(snapshot.WaitlistedLearnerIds));
    }

    [Fact]
    public void Drop_PromotesInFifoOrder()
    {
        var offering = new CourseOffering(1, _ => true);
        offering.Enroll("A");
        offering.Enroll("B");
        offering.Enroll("C");

        offering.Drop("A");

        Assert.Equal(new[] { "B" }, offering.GetSnapshot().EnrolledLearnerIds);
        Assert.Equal(new[] { "C" }, offering.GetSnapshot().WaitlistedLearnerIds);
    }

    [Fact]
    public void IneligibleLearner_IsRejectedAtEnrollment()
    {
        var offering = new CourseOffering(1, learner => learner != "NoPrerequisite");

        Assert.Equal(EnrollmentResult.Ineligible, offering.Enroll("NoPrerequisite"));
        Assert.Empty(offering.GetSnapshot().EnrolledLearnerIds);
        Assert.Empty(offering.GetSnapshot().WaitlistedLearnerIds);
    }

    [Fact]
    public void Promotion_RemovesIneligibleLearnerFromBothWaitlistStructures()
    {
        var eligibility = new Dictionary<string, bool>
        {
            ["A"] = true, ["B"] = true, ["C"] = true
        };
        var offering = new CourseOffering(1, learner => eligibility[learner]);
        offering.Enroll("A");
        offering.Enroll("B");
        offering.Enroll("C");
        eligibility["B"] = false;

        offering.Drop("A");

        Assert.Equal(new[] { "C" }, offering.GetSnapshot().EnrolledLearnerIds);
        Assert.Empty(offering.GetSnapshot().WaitlistedLearnerIds);

        // If B remained in duplicate-detection state, this would return AlreadyActive.
        eligibility["B"] = true;
        Assert.Equal(EnrollmentResult.Waitlisted, offering.Enroll("B"));
        offering.Drop("C");
        Assert.Equal(new[] { "B" }, offering.GetSnapshot().EnrolledLearnerIds);
    }

    [Fact]
    public void RemovingMiddleWaitlistLearner_PreservesRemainingFifoOrder()
    {
        var eligibility = new Dictionary<string, bool>
        {
            ["A"] = true, ["B"] = true, ["C"] = true,
            ["D"] = true, ["E"] = true, ["F"] = true
        };
        var offering = new CourseOffering(1, learner => eligibility[learner]);
        offering.Enroll("A");
        offering.Enroll("B");
        offering.Enroll("C");
        offering.Enroll("D");
        offering.Enroll("E");
        offering.Enroll("F");
        eligibility["B"] = false;
        eligibility["C"] = false;

        offering.Drop("A");

        Assert.Equal(new[] { "D" }, offering.GetSnapshot().EnrolledLearnerIds);
        Assert.Equal(new[] { "E", "F" }, offering.GetSnapshot().WaitlistedLearnerIds);

        offering.Drop("D");
        Assert.Equal(new[] { "E" }, offering.GetSnapshot().EnrolledLearnerIds);
        Assert.Equal(new[] { "F" }, offering.GetSnapshot().WaitlistedLearnerIds);
    }

    [Fact]
    public void PromotedLearnerLeavesBothWaitlistStructures_AndLaterDropsRemainFifo()
    {
        var offering = new CourseOffering(1, _ => true);
        offering.Enroll("A");
        offering.Enroll("B");
        offering.Enroll("C");

        offering.Drop("A"); // B promoted
        Assert.Equal(new[] { "C" }, offering.GetSnapshot().WaitlistedLearnerIds);
        Assert.Equal(EnrollmentResult.AlreadyActive, offering.Enroll("B"));

        offering.Drop("B"); // C promoted next
        Assert.Equal(new[] { "C" }, offering.GetSnapshot().EnrolledLearnerIds);
        Assert.Equal(EnrollmentResult.Waitlisted, offering.Enroll("B"));
        Assert.Equal(new[] { "B" }, offering.GetSnapshot().WaitlistedLearnerIds);

        offering.Drop("C"); // B re-entered at the tail and is now promoted
        Assert.Equal(new[] { "B" }, offering.GetSnapshot().EnrolledLearnerIds);
        Assert.Empty(offering.GetSnapshot().WaitlistedLearnerIds);
    }

    [Fact]
    public void DropWithNoEligibleWaitlistedLearner_LeavesSeatOpen()
    {
        var eligible = true;
        var offering = new CourseOffering(1, _ => eligible);
        offering.Enroll("A");
        offering.Enroll("B");
        eligible = false;

        Assert.Equal(DropResult.Dropped, offering.Drop("A"));
        Assert.Empty(offering.GetSnapshot().EnrolledLearnerIds);
        Assert.Empty(offering.GetSnapshot().WaitlistedLearnerIds);
    }

    [Fact]
    public void InvalidOrRepeatedDrops_DoNotChangeState()
    {
        var offering = new CourseOffering(1, _ => true);
        offering.Enroll("A");
        var before = offering.GetSnapshot();

        Assert.Equal(DropResult.InvalidRequest, offering.Drop(" "));
        Assert.Equal(DropResult.NotEnrolled, offering.Drop("missing"));
        Assert.Equal(DropResult.Dropped, offering.Drop("A"));
        var afterFirstDrop = offering.GetSnapshot();
        Assert.Equal(DropResult.NotEnrolled, offering.Drop("A"));

        Assert.Equal(before.EnrolledLearnerIds, new[] { "A" });
        Assert.Equal(afterFirstDrop.EnrolledLearnerIds, offering.GetSnapshot().EnrolledLearnerIds);
        Assert.Equal(afterFirstDrop.WaitlistedLearnerIds, offering.GetSnapshot().WaitlistedLearnerIds);
    }

    [Fact]
    public void WaitlistedLearnerCannotBeDropped()
    {
        var offering = new CourseOffering(1, _ => true);
        offering.Enroll("A");
        offering.Enroll("B");

        Assert.Equal(DropResult.NotEnrolled, offering.Drop("B"));
        Assert.Equal(new[] { "B" }, offering.GetSnapshot().WaitlistedLearnerIds);
    }

    [Fact]
    public void DroppedLearnerCanSubmitNewEnrollmentRequest()
    {
        var offering = new CourseOffering(1, _ => true);
        offering.Enroll("A");
        offering.Drop("A");

        Assert.Equal(EnrollmentResult.Enrolled, offering.Enroll("A"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_RejectsNonPositiveCapacity(int capacity)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CourseOffering(capacity, _ => true));
    }

    [Fact]
    public void InvalidLearnerId_DoesNotChangeState()
    {
        var offering = new CourseOffering(1, _ => true);

        Assert.Equal(EnrollmentResult.InvalidRequest, offering.Enroll(null));
        Assert.Equal(EnrollmentResult.InvalidRequest, offering.Enroll("  "));
        Assert.Empty(offering.GetSnapshot().EnrolledLearnerIds);
        Assert.Empty(offering.GetSnapshot().WaitlistedLearnerIds);
    }
}
