# AI prompt and use record

This document records the significant AI-assisted interactions used during development of the CourseRegistration assessment. The prompts are reproduced from the prompts supplied to Codex during development. Response descriptions are summaries rather than verbatim AI responses.

The record documents the prompts, resulting suggestions, developer decisions, affected files, and available verification evidence. It focuses on material development decisions rather than every interaction.
## Prompts used

### Prompt 1 — Project brief and collaboration rules

**AI tool:** Codex.

```text
Act as a Senior Software Engineer and .NET Technical Lead helping me complete an AI-assisted engineering assessment for an interview.
PROJECT:
Course Registration — Enrollment, Drop, and FIFO Waitlist Promotion.
TECHNOLOGY:
* C#
* .NET
* xUnit for automated tests, unless an existing project convention or a compelling technical reason suggests otherwise.
* In-memory persistence is acceptable because no starter project or database has been provided.
YOUR ROLE:
Work as an engineering partner, not as an autonomous code generator. Help me understand the requirements, make sound engineering decisions, and produce a reliable, maintainable implementation.
ENGINEERING PRINCIPLES:
1. Understand before implementing.
Inspect the existing workspace and project files before proposing changes. Do not assume that files, dependencies, or infrastructure exist.
2. Preserve simplicity.
Implement only the functionality required by the assessment. Avoid unnecessary abstractions, design patterns, packages, databases, frameworks, and infrastructure.
3. Architectural changes require approval.
Never make significant architectural changes without first explaining:
    * The current design.
    * The proposed change.
    * Why it is needed.
    * The alternatives.
    * The benefits and trade-offs.
    * The files that would be affected.
Then wait for my explicit approval.
4. Work incrementally.
Break the work into small, reviewable tasks. Before each major implementation stage, explain the intended changes. Do not rewrite the entire project unnecessarily.
5. Protect correctness.
Pay particular attention to capacity limits, duplicate enrollment, prerequisite eligibility, FIFO ordering, repeated drops, invalid requests, and consistency during concurrent operations.
6. Test your work.
Create focused automated tests, execute them where possible, inspect failures, and report actual results. Never claim that tests passed unless you executed them and observed the result.
7. Challenge your own output.
Identify edge cases, assumptions, potential bugs, and weaknesses in your proposed solution. Do not assume that generated code is correct merely because it compiles.
8. Explain important decisions.
Explain the reasoning behind business rules, data structures, synchronization, and error handling.
9. Protect the workspace.
Do not delete files, overwrite unrelated work, introduce unnecessary dependencies, or run destructive commands without asking first.
10. Follow the assessment scope.
Do not add a frontend, authentication, payments, a course catalog, multi-term registration, deployment, or a new database unless the assessment or existing project genuinely requires it.
INTERACTION RULES:
* Ask me questions only when a missing decision materially affects correctness or architecture.
* If an assumption is reasonable and low-risk, state it and proceed with the analysis.
* Before making a significant architectural change, always ask for my approval.
* Do not start implementing until I have reviewed and approved the implementation plan.
* Do not mark a task complete without explaining how it was verified.
For now, acknowledge these instructions and inspect the current workspace without modifying any files. Report what you find and wait for my next instruction.
```

**AI response summary:** Inspected the workspace without changes and reported that it was empty, with no project files, conventions, or Git repository.

**Owner decision/use:** The read-only inspection was requested and accepted as the first step; implementation was held until requirements and architecture were reviewed and approved.

**Files/decisions informed:** No files changed at this stage. The empty workspace informed the later solution structure and use of .NET 8 with xUnit.

### Prompt 2 — Assessment requirements and analysis

**AI tool:** Codex.

```text
Analyze the following assessment requirements without creating or modifying any files.
OBJECTIVE:
Implement enrollment, dropping, and FIFO waitlist promotion for one course offering.
REQUIREMENTS:
1. Enrollment succeeds when capacity is available and the learner satisfies the prerequisite rule.
2. When the course is full, an eligible learner joins the waitlist exactly once.
3. Duplicate enrollment or waitlist requests must not create duplicate active records.
4. When an enrolled learner drops, the earliest eligible waitlisted learner is promoted.
5. An ineligible learner must not be enrolled or promoted.
6. Invalid requests and repeated drops must return clear results without corrupting enrollment, capacity, or waitlist state.
7. Tests must cover available capacity, full capacity, duplicates, FIFO promotion, prerequisite failures, and invalid or repeated drops.
8. Preserve appropriate transaction or locking behavior if required by the persistence model.
9. Document the prerequisite and waitlist assumptions, including what happens when an ineligible learner reaches the front of the queue.
10. Provide runnable code, automated tests, a design specification, an AI prompt and usage record, setup instructions, and a concise limitations summary.
IMPORTANT SCOPE RESTRICTIONS:
Implement only one course offering’s enrollment, drop, and waitlist-promotion functionality.
Do not introduce a frontend, authentication, payments, a catalog, multi-term registration, deployment, or a new database.
TASK:
A. Translate the requirements into explicit business rules.
B. Identify the important invariants that must always hold.
C. Identify ambiguous requirements and recommend reasonable assumptions.
D. List the essential edge cases and corresponding tests.
E. Identify potential concurrency issues relevant to an in-memory implementation.
F. Distinguish mandatory acceptance criteria from optional improvements.
Do not implement anything yet. Present your analysis in a structured format and wait for my review.
```

**AI response summary:** Translated the requirements into business rules, invariants, ambiguities, edge cases, concurrency issues, and mandatory versus optional acceptance criteria. Identified possible policies for an ineligible learner at the front of the waitlist.

**Owner decision/use:** The owner accepted the analysis and supplied the definitive business decisions in Prompt 3. The owner chose to remove a now-ineligible waitlisted learner and continue scanning, superseding the earlier possibility of leaving that learner queued.

**Files/decisions informed:** These rules informed `DESIGN.md`, `src/CourseRegistration/CourseOffering.cs`, and `tests/CourseRegistration.Tests/CourseOfferingTests.cs`. No files were created during this analysis-only step.

### Prompt 3 — Business decisions and architecture proposal

**AI tool:** Codex.

```text
Your requirements analysis is good. Before proceeding, adopt to the following decisions:
1. Reject an ineligible learner at enrollment time. Do not add them to the waitlist.
2. During promotion, recheck eligibility. If a previously waitlisted learner is now ineligible, skip them and remove them from the active waitlist. Continue to the next eligible learner.
3. Duplicate requests must not change existing enrollment state or waitlist position.
4. A learner who previously dropped may submit a new enrollment request.
5. Capacity is fixed and must be positive.
6. Use an in-memory store with synchronization appropriate for a single-process implementation.
7. Keep the solution minimal and avoid unnecessary abstractions or dependencies.
Now propose the architecture and implementation plan for the empty workspace.
Include the project structure, class responsibilities, data model, operation workflows, concurrency approach, and planned tests.
Do not create or modify files yet. Explain any significant trade-offs and wait for my approval.
```

**AI response summary:** Proposed a minimal class library and xUnit project, one `CourseOffering`, result enums, an eligibility predicate, in-memory enrolled and waitlist state, a snapshot, and one lock around state transitions. The linked list supplies FIFO order; a hash set supports duplicate checks.

**Owner decision/use:** The proposed design was accepted in Prompt 4. The owner retained the small in-memory design and added required consistency and FIFO test details. This approval authorized implementation after those details were incorporated.

**Files/decisions informed:** `CourseRegistration.sln`; `src/CourseRegistration/CourseRegistration.csproj`; `src/CourseRegistration/CourseOffering.cs`; `src/CourseRegistration/RegistrationResult.cs`; `tests/CourseRegistration.Tests/CourseRegistration.Tests.csproj`; `DESIGN.md`.

### Prompt 4 — Design refinements and required tests

**AI tool:** Codex.

```text
The proposed architecture looks good.
Before implementation, please ensure that whenever a learner is removed from the waitlist—whether because they are ineligible or because they are promoted—the learner is removed from both the LinkedList and the HashSet used for duplicate detection.
The LinkedList should preserve FIFO order without storing or manually updating position numbers.
Also include tests verifying that:
1. Removing a learner from the middle of the waitlist preserves the relative order of the remaining learners.
2. Removing an ineligible learner also removes them from duplicate-detection state.
3. A promoted learner is removed from both waitlist structures.
4. The remaining eligible learners are promoted in the correct FIFO order on subsequent drops.
Do not introduce a separate position-tracking mechanism. Otherwise, keep the approved architecture unchanged.
```

**AI response summary:** Implemented the approved service with a shared waitlist-removal operation that updates both the `LinkedList` and duplicate-detection `HashSet`; added xUnit coverage for the specified removal and FIFO cases.

**Owner decision/use:** The owner’s design approval and added requirements were accepted. FIFO stayed represented by linked-list order; no position-tracking mechanism was added.

**Files/decisions informed:** `src/CourseRegistration/CourseOffering.cs`, `tests/CourseRegistration.Tests/CourseOfferingTests.cs`, `DESIGN.md`, and `README.md`.

### Prompt 5 — Final implementation audit

**AI tool:** Codex.

```text
Before submission, perform a final audit against the original Course Waitlist assessment.
Check the actual source code, tests, and documentation.
Verify:
1. Test coverage for all mandatory acceptance criteria.
2. Accuracy of DESIGN.md, AI_USAGE.md, and README.md.
3. Whether the project can be built and tested using the documented commands.
Also check that AI_USAGE.md accurately represents the actual prompts, decisions, and verification performed.
Do not modify any code files during this audit. Modify md files accordingly if required.
Return a table mapping each acceptance criterion to the implemented, relevant test case, and verification status. Identify any gaps or risks that must be addressed before submission
```

**AI response summary:** Audited the source and tests against mandatory criteria, checked documentation, and mapped criteria to implementation, tests, and verification status. Identified documentation accuracy and execution environment as audit points.

**Owner decision/use:** The owner allowed Markdown corrections while prohibiting code changes for this audit. That direction was followed; later documentation passes corrected prompt and verification descriptions.

**Files/decisions informed:** `AI_USAGE.md`, `DESIGN.md`, and `README.md`; audited `src/CourseRegistration/CourseOffering.cs` and `tests/CourseRegistration.Tests/CourseOfferingTests.cs`.

### Prompt 6 — Concurrent last-seat test design

**AI tool:** Codex.

```text
Propose one focused automated test that starts two eligible learners attempting to enroll concurrently when exactly one seat remains.
The test should verify:
* Exactly one learner is enrolled in the remaining seat.
* The other learner is waitlisted.
* The course never exceeds the capacity.
* No learner appears in both the enrolled collection and the waitlist.
First explain the test design and how it will coordinate the concurrent operations. Do not modify files until I approve the design approach.
```

**AI response summary:** Proposed synchronizing two enrollment tasks and the test thread with a three-party `Barrier`, releasing both contenders together, then checking one snapshot. Either learner may win the final seat.

**Owner decision/use:** The owner approved the barrier approach in Prompt 7, including the capacity and disjoint-state assertions.

**Files/decisions informed:** `tests/CourseRegistration.Tests/CourseOfferingTests.cs`, specifically `ConcurrentEnrollments_WithOneSeatRemaining_EnrollsExactlyOneLearner`; test-coverage notes in `README.md` and `DESIGN.md`.

### Prompt 7 — Approval and verification request

**AI tool:** Codex.

```text
Approved.
After implementation, verify that all test cases are working fine and nothing is effected.
```

**AI response summary:** Added the approved concurrency test and ran the full xUnit suite.

**Owner decision/use:** Approval authorized the test implementation and requested verification. The final observed run passed all 16 tests.

**Files/decisions informed:** `tests/CourseRegistration.Tests/CourseOfferingTests.cs`; `AI_USAGE.md` verification record.

### Prompt 8 — Markdown audit

**AI tool:** Codex.

```text
Now, verify all the md files and update them accordingly with the latest cahnges if needed.
```

**AI response summary:** Reviewed Markdown claims against the implementation and latest test result, then updated setup, design, and usage notes.

**Owner decision/use:** The owner requested Markdown-only updates for the latest changes; that scope was followed.

**Files/decisions informed:** `README.md`, `DESIGN.md`, and `AI_USAGE.md`.

## Related automated tests

The suite also covers available capacity, full waitlisting, duplicate requests, FIFO promotion, prerequisite failures and eligibility changes, waitlist cleanup and order, invalid or repeated drops, re-enrollment, and positive capacity. Test names include `Enroll_UsesAvailableCapacity`, `Enroll_WhenFull_AddsEligibleLearnerToWaitlist`, `DuplicateRequests_DoNotChangeEnrollmentOrWaitlistPosition`, `Drop_PromotesInFifoOrder`, `IneligibleLearner_IsRejectedAtEnrollment`, `Promotion_RemovesIneligibleLearnerFromBothWaitlistStructures`, `RemovingMiddleWaitlistLearner_PreservesRemainingFifoOrder`, `PromotedLearnerLeavesBothWaitlistStructures_AndLaterDropsRemainFifo`, `DropWithNoEligibleWaitlistedLearner_LeavesSeatOpen`, `InvalidOrRepeatedDrops_DoNotChangeState`, `WaitlistedLearnerCannotBeDropped`, `DroppedLearnerCanSubmitNewEnrollmentRequest`, `Constructor_RejectsNonPositiveCapacity`, and `InvalidLearnerId_DoesNotChangeState`.

## Verification evidence

Latest local results reported by the owner:

- `dotnet build CourseRegistration.sln` — succeeded with 0 warnings and 0 errors.
- `dotnet test CourseRegistration.sln` — 16 passed, 0 failed, 0 skipped.

Terminal output visible earlier in this thread also recorded:

- `DOTNET_CLI_HOME=/tmp/coursereg-dotnet DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 dotnet build CourseRegistration.sln -m:1` — build succeeded with 0 errors; NuGet emitted `NU1900` because vulnerability metadata was unreachable in that restricted environment.
- `DOTNET_CLI_HOME=/tmp/coursereg-dotnet DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 dotnet test CourseRegistration.sln -m:1` — 16 passed, 0 failed, 0 skipped; output included the environment-specific NuGet warning.
