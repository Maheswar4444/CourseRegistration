namespace CourseRegistration;

public enum EnrollmentResult
{
    Enrolled,
    Waitlisted,
    AlreadyActive,
    Ineligible,
    InvalidRequest
}

public enum DropResult
{
    Dropped,
    NotEnrolled,
    InvalidRequest
}

public sealed record CourseOfferingSnapshot(
    IReadOnlyList<string> EnrolledLearnerIds,
    IReadOnlyList<string> WaitlistedLearnerIds,
    int Capacity);
