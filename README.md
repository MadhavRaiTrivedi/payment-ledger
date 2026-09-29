# Payment Ledger

A wallet backend that records every money movement as double-entry ledger entries, so balances can always be traced, retried requests never charge twice, and the books always add up to zero.

## Why I built it

Most simple wallet apps keep one `balance` column and update it in place. That loses the history of why a balance changed, breaks when two requests update the same account at the same time, and gives an auditor nothing to check.

Real payment systems solve this with double-entry bookkeeping: every transaction is a set of debit and credit entries that sum to zero, entries are never edited, and balances are calculated from them. I built this project to learn how that works in practice, and to get hands-on with the problems that come with moving money over an API: handling concurrent requests safely, making retries idempotent, and publishing events reliably after a database write.

## Status

Requirements are written. Implementation has not started yet.

- [Requirements](docs/requirements.md): scope, functional and non-functional requirements, and the decisions taken so far.

## Planned features

- Customer wallets and system accounts (`Settlement`, `FeeRevenue`, `Suspense`)
- Deposits, withdrawals and transfers between wallets, with a transfer fee
- Full and partial refunds, and reversals of posted transactions
- Holds that reserve money before it is captured
- Account statements and balance as of any past date
- Safe retries with an `Idempotency-Key` header
- Events published to RabbitMQ through a transactional outbox
- A scheduled reconciliation job that checks the ledger always sums to zero

## Planned stack

.NET 10, PostgreSQL, RabbitMQ, Redis, Docker. Each choice will be explained in a tech stack table once it is in the code.
