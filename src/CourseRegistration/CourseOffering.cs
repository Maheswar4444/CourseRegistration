namespace CourseRegistration;

/// <summary>Manages enrollment for one course offering in a single process.</summary>
public sealed class CourseOffering
{
    private readonly object _sync = new();
    private readonly Func<string, bool> _isEligible;
    private readonly HashSet<string> _enrolled = new(StringComparer.Ordinal);
    private readonly LinkedList<string> _waitlist = new();
    private readonly HashSet<string> _waitlisted = new(StringComparer.Ordinal);

    public CourseOffering(int capacity, Func<string, bool> isEligible)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
        }

        _isEligible = isEligible ?? throw new ArgumentNullException(nameof(isEligible));
        Capacity = capacity;
    }

    public int Capacity { get; }

    public EnrollmentResult Enroll(string? learnerId)
    {
        if (!IsValidLearnerId(learnerId))
        {
            return EnrollmentResult.InvalidRequest;
        }

        lock (_sync)
        {
            if (_enrolled.Contains(learnerId!) || _waitlisted.Contains(learnerId!))
            {
                return EnrollmentResult.AlreadyActive;
            }

            if (!_isEligible(learnerId!))
            {
                return EnrollmentResult.Ineligible;
            }

            if (_enrolled.Count < Capacity)
            {
                _enrolled.Add(learnerId!);
                return EnrollmentResult.Enrolled;
            }

            _waitlist.AddLast(learnerId!);
            _waitlisted.Add(learnerId!);
            return EnrollmentResult.Waitlisted;
        }
    }

    public DropResult Drop(string? learnerId)
    {
        if (!IsValidLearnerId(learnerId))
        {
            return DropResult.InvalidRequest;
        }

        lock (_sync)
        {
            if (!_enrolled.Remove(learnerId!))
            {
                return DropResult.NotEnrolled;
            }

            PromoteEarliestEligible();
            return DropResult.Dropped;
        }
    }

    public CourseOfferingSnapshot GetSnapshot()
    {
        lock (_sync)
        {
            return new CourseOfferingSnapshot(_enrolled.ToArray(), _waitlist.ToArray(), Capacity);
        }
    }

    private void PromoteEarliestEligible()
    {
        var node = _waitlist.First;
        while (node is not null)
        {
            var next = node.Next;
            if (!_isEligible(node.Value))
            {
                RemoveFromWaitlist(node);
            }
            else
            {
                var learnerId = node.Value;
                RemoveFromWaitlist(node);
                _enrolled.Add(learnerId);
                return;
            }

            node = next;
        }
    }

    private void RemoveFromWaitlist(LinkedListNode<string> node)
    {
        _waitlist.Remove(node);
        _waitlisted.Remove(node.Value);
    }

    private static bool IsValidLearnerId(string? learnerId) =>
        !string.IsNullOrWhiteSpace(learnerId);
}
