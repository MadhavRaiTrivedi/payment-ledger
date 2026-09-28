# Payment Ledger: Requirements

## 1. Problem statement

A digital wallet needs to move money between accounts and prove, at any moment, where every rupee is. Storing a single `balance` column and updating it in place loses history, breaks under concurrent requests, and cannot be audited.

Payment Ledger is a backend service that records every money movement as immutable double-entry postings. Balances are derived from the entries, duplicate requests are safe, and the books always sum to zero.

## 2. Actors

| Actor | Description |
|---|---|
| Customer | Owns one or more wallet accounts. Deposits, withdraws, transfers and views statements. |
| Operations admin | Manages accounts, issues refunds, reviews reconciliation reports. |
| System | Background workers: outbox publisher, hold expiry, reconciliation job. |

## 3. Core concepts

- **Account**: a customer wallet or a system account. System accounts represent money outside customer wallets:
  - `Settlement`: money entering or leaving the platform (bank deposits and withdrawals).
  - `FeeRevenue`: fees charged by the platform.
  - `Suspense`: temporary holding for money whose destination is not yet known.
- **Transaction**: one business operation (a transfer, a deposit, a refund). It contains two or more entries.
- **Entry**: a single debit or credit against one account. Entries of a transaction always sum to zero.
- **Balance**: the sum of an account's posted entries. It is never stored as the source of truth.

## 4. Functional requirements

Priority uses MoSCoW: **M**ust, **S**hould, **C**ould.

### Accounts

| ID | Requirement | Priority |
|---|---|---|
| FR-01 | An admin can open a customer wallet account for a customer. | M |
| FR-02 | System accounts (`Settlement`, `FeeRevenue`, `Suspense`) are created at startup and cannot be closed. | M |
| FR-03 | An account has a status: `Active`, `Frozen` or `Closed`. Frozen accounts cannot send or receive money. Closed accounts must have zero balance. | M |
| FR-04 | A customer can view the current balance and available balance (balance minus active holds) of their accounts. | M |

### Money movement

| ID | Requirement | Priority |
|---|---|---|
| FR-05 | **Deposit**: credit a customer wallet, debit `Settlement`. | M |
| FR-06 | **Withdrawal**: debit a customer wallet, credit `Settlement`. Rejected if available balance is insufficient. | M |
| FR-07 | **Transfer**: move money between two customer wallets. Rejected if available balance is insufficient or either account is not `Active`. | M |
| FR-08 | **Transfer fee**: a configurable fee is charged on transfers and credited to `FeeRevenue`, in the same transaction as the transfer. | S |
| FR-09 | **Refund**: an admin can refund a posted transfer, fully or partially. Total refunds cannot exceed the original amount. A refund is a new transaction, never an edit of the original. | M |
| FR-10 | **Reversal**: an admin can reverse a posted transaction by posting equal and opposite entries. The original is marked `Reversed`. | M |
| FR-11 | **Hold**: reserve an amount on a wallet without moving it. A hold can be captured (becomes a posted transaction), released, or expires automatically after a configured time. | S |

### Transaction lifecycle

| ID | Requirement | Priority |
|---|---|---|
| FR-12 | A transaction has a status: `Pending`, `Posted`, `Failed` or `Reversed`. | M |
| FR-13 | Allowed transitions: `Pending → Posted`, `Pending → Failed`, `Posted → Reversed`. Any other transition is rejected. | M |

### History and reporting

| ID | Requirement | Priority |
|---|---|---|
| FR-14 | A customer can view a paginated statement of an account's entries, filtered by date range. | M |
| FR-15 | A customer can query an account's balance as of any past date and time. | S |
| FR-16 | An admin can look up any transaction with all its entries. | M |

### Events and reconciliation

| ID | Requirement | Priority |
|---|---|---|
| FR-17 | Every posted, failed or reversed transaction publishes a domain event to RabbitMQ (`TransactionPosted`, `TransactionFailed`, `TransactionReversed`). | M |
| FR-18 | A scheduled reconciliation job verifies that all entries sum to zero and that each account's derived balance matches its balance snapshot. Mismatches are reported and raise an alert. | M |
| FR-19 | A dashboard shows accounts, balances, recent transactions and the last reconciliation result. | C |

## 5. Non-functional requirements

| ID | Requirement |
|---|---|
| NFR-01 | **Zero-sum invariant**: the entries of every transaction sum to zero. The service rejects any transaction that violates this, and the database enforces it as a second line of defence. |
| NFR-02 | **Immutability**: entries are never updated or deleted. Corrections are made only through reversals and refunds. |
| NFR-03 | **Idempotency**: every money-moving request carries an `Idempotency-Key` header. Repeating a request with the same key returns the original response and moves no money. Reusing a key with a different request body returns `422`. Keys are kept for 24 hours. |
| NFR-04 | **Concurrency**: parallel requests against the same account never produce lost updates or a negative available balance. |
| NFR-05 | **Money representation**: amounts are stored as integer minor units (paise) with an explicit currency. No floating-point arithmetic on money. |
| NFR-06 | **Reliable events**: events are written to an outbox table in the same database transaction as the entries, and published asynchronously. A crash never loses an event or publishes one for a rolled-back transaction. Consumers must tolerate duplicates. |
| NFR-07 | **Auditability**: every transaction records who initiated it, when, the request's correlation ID and the idempotency key. |
| NFR-08 | **Performance target**: 500 transfers per second with p95 latency under 50 ms on a single developer machine, verified with a k6 load test. |
| NFR-09 | **Observability**: structured logs with correlation IDs, OpenTelemetry traces across API, database and RabbitMQ, and metrics for transaction throughput, failures and outbox lag. |
| NFR-10 | **Security**: JWT authentication with `Customer` and `Admin` roles. Customers can access only their own accounts. |
| NFR-11 | **Testability**: domain rules covered by unit tests; money-movement flows, idempotency and concurrency covered by integration tests against real PostgreSQL, RabbitMQ and Redis (Testcontainers). |
| NFR-12 | **Local setup**: the whole system runs with `docker compose up`. |

## 6. Out of scope

- Integration with real banks, card networks or payment gateways. Deposits and withdrawals are simulated against the `Settlement` account.
- KYC, fraud detection and transaction limits by regulation.
- Multiple currencies and foreign exchange. The `Money` type carries a currency so this can be added later, but only INR is accepted.
- Interest, scheduled or recurring payments.
- Full identity provider (OAuth, SSO). Tokens are issued by a simple development endpoint.
- Multi-region deployment and database sharding.

## 7. Glossary

| Term | Meaning |
|---|---|
| Double-entry | Every movement is recorded as at least one debit and one credit that sum to zero. |
| Entry / posting | One debit or credit line against one account. |
| Available balance | Posted balance minus active holds. |
| Hold | A reservation of funds that is not yet a posted transaction. |
| Idempotency key | A client-generated unique key that makes a retried request safe. |
| Outbox | A table where events are saved in the same database transaction as the data change, then published by a separate worker. |
| Reconciliation | Checking that the ledger's totals and balances are internally consistent. |
| Minor units | The smallest currency unit (paise for INR); 100 paise = 1 rupee. |

## 8. Decisions taken

These defaults were chosen to keep scope realistic. Revisit before implementation starts.

| Decision | Choice | Reason |
|---|---|---|
| Currencies | INR only | Keeps focus on ledger correctness, not FX. |
| Holds | Should-have, built after core flows | Strong interview topic, but not needed for the first working version. |
| Transfer fee | Flat fee from configuration | Shows a multi-entry transaction without a pricing engine. |
| Auth | Simple JWT with roles | Enough to show authorization rules without building an identity server. |
| Balance storage | Derived from entries, with a snapshot table for fast reads | Correct by construction, still fast; reconciliation checks the snapshot. |
| Idempotency key retention | 24 hours | Common industry default. |
