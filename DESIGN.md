# Design specification

## Scope

Manage enrollment, drops, and waitlist promotion for one course offering in a single process. The offering has a fixed, positive capacity. Persistence is in memory only.

## Business rules

- An eligible learner enrolls immediately when a seat is available.
- An eligible learner joins the tail of the waitlist when the offering is full.
- An ineligible learner is rejected at enrollment and is not waitlisted.
- A learner can have at most one active record. Duplicate requests return `AlreadyActive` without changing enrollment or queue position.
- Dropping an enrolled learner frees one seat. The waitlist is scanned in FIFO order; the earliest learner still eligible is promoted.
- Eligibility is checked again during promotion. A now-ineligible learner is removed from the active waitlist and scanning continues.
- If no eligible learner remains, the seat stays open.
- A learner removed by a successful drop or skipped as ineligible may make a new enrollment request later.
- Invalid learner IDs and drops for non-enrolled learners return explicit results without changing state.

## Invariants and synchronization

- Enrolled learners never exceed capacity.
- A learner cannot be both enrolled and waitlisted.
- The linked list is the source of FIFO order. No position numbers are stored.
- Every waitlist removal updates both the linked list and duplicate-detection set while holding the same lock.
- A single private lock protects state checks, updates, promotion, and snapshots. This is process-local synchronization, not distributed coordination.

## Prerequisite and eligibility assumption

The caller supplies an eligibility predicate keyed by learner ID. The offering invokes it during enrollment and again when considering a learner for promotion. The predicate is expected to be fast and not re-enter this offering. Eligibility changes are therefore reflected at the next promotion attempt.

## Verification coverage

The xUnit suite includes a coordinated concurrent enrollment case with one remaining seat. It verifies that one contender enrolls, the other waits, capacity is not exceeded, and no learner appears in both collections. Other tests cover the business rules and waitlist removal behavior above.

## Limitations

State is lost when the process exits. The lock does not coordinate multiple processes or machines. There is no catalog, term model, learner profile, external prerequisite store, or persistence layer.
