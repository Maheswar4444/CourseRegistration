# Course Registration

Minimal in-memory implementation of enrollment, dropping, and FIFO waitlist promotion for one course offering.

## Requirements

- .NET 8 SDK

## Build and run the automated tests

From the repository root:

```sh
dotnet build CourseRegistration.sln -m:1
dotnet test CourseRegistration.sln -m:1
```

`-m:1` runs MSBuild with one worker. Package restore requires access to NuGet on the first run.

## Use the library

Create a `CourseOffering` with a positive capacity and an eligibility predicate, then call `Enroll(learnerId)` or `Drop(learnerId)`. `GetSnapshot()` returns a point-in-time view of enrolled and waitlisted IDs.

The xUnit suite covers capacity, duplicate requests, eligibility changes, drops, FIFO promotion, waitlist cleanup, and two learners racing for the last seat.

See [DESIGN.md](DESIGN.md) for business rules, synchronization, assumptions, and limitations.
