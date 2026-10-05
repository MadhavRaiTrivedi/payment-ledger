using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PaymentLedger.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    balance_in_paise = table.Column<long>(type: "bigint", nullable: false),
                    held_in_paise = table.Column<long>(type: "bigint", nullable: false),
                    opened_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_accounts", x => x.id);
                    table.CheckConstraint("ck_accounts_wallet_not_overdrawn", "type <> 'CustomerWallet' OR (held_in_paise >= 0 AND balance_in_paise >= held_in_paise)");
                });

            migrationBuilder.CreateTable(
                name: "idempotency_records",
                columns: table => new
                {
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    request_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    response_body = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_idempotency_records", x => new { x.owner_id, x.idempotency_key });
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    publish_attempts = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "processed_messages",
                columns: table => new
                {
                    message_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consumer = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processed_messages", x => new { x.consumer, x.message_id });
                });

            migrationBuilder.CreateTable(
                name: "reconciliation_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outcome = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ledger_total_in_paise = table.Column<long>(type: "bigint", nullable: false),
                    wallet_mismatches = table.Column<string>(type: "jsonb", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reconciliation_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    source_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    amount_in_paise = table.Column<long>(type: "bigint", nullable: false),
                    fee_in_paise = table.Column<long>(type: "bigint", nullable: false),
                    refunded_in_paise = table.Column<long>(type: "bigint", nullable: false),
                    original_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reversal_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    initiated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_transactions", x => x.id);
                    table.CheckConstraint("ck_transactions_refund_within_amount", "refunded_in_paise BETWEEN 0 AND amount_in_paise");
                });

            migrationBuilder.CreateTable(
                name: "holds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    beneficiary_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount_in_paise = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    capture_transaction_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_holds", x => x.id);
                    table.ForeignKey(
                        name: "fk_holds_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_holds_accounts_beneficiary_account_id",
                        column: x => x.beneficiary_account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "entries",
                columns: table => new
                {
                    sequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    direction = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    amount_in_paise = table.Column<long>(type: "bigint", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    balance_after_in_paise = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_entries", x => x.sequence);
                    table.CheckConstraint("ck_entries_amount_positive", "amount_in_paise > 0");
                    table.ForeignKey(
                        name: "fk_entries_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_entries_transactions_transaction_id",
                        column: x => x.transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "accounts",
                columns: new[] { "id", "balance_in_paise", "currency", "held_in_paise", "opened_at", "owner_id", "status", "type" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), 0L, "INR", 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Active", "Settlement" },
                    { new Guid("00000000-0000-0000-0000-000000000002"), 0L, "INR", 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Active", "FeeRevenue" },
                    { new Guid("00000000-0000-0000-0000-000000000003"), 0L, "INR", 0L, new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, "Active", "Suspense" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_accounts_owner_id",
                table: "accounts",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_entries_account_id_sequence",
                table: "entries",
                columns: new[] { "account_id", "sequence" });

            migrationBuilder.CreateIndex(
                name: "ix_entries_transaction_id",
                table: "entries",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_holds_account_id",
                table: "holds",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "ix_holds_active_expires_at",
                table: "holds",
                column: "expires_at",
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_holds_beneficiary_account_id",
                table: "holds",
                column: "beneficiary_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_records_expires_at",
                table: "idempotency_records",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_unpublished",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "published_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_reconciliation_runs_started_at",
                table: "reconciliation_runs",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "ix_transactions_original_transaction_id",
                table: "transactions",
                column: "original_transaction_id");

            // The domain already refuses unbalanced or edited entries. These triggers make the database
            // refuse them too, so a bug or a manual SQL fix cannot break the ledger.
            migrationBuilder.Sql("""
                CREATE FUNCTION ensure_transaction_balanced() RETURNS trigger
                LANGUAGE plpgsql AS $$
                DECLARE
                    imbalance bigint;
                BEGIN
                    SELECT COALESCE(SUM(CASE WHEN direction = 'Credit' THEN amount_in_paise ELSE -amount_in_paise END), 0)
                    INTO imbalance
                    FROM entries
                    WHERE transaction_id = NEW.transaction_id;

                    IF imbalance <> 0 THEN
                        RAISE EXCEPTION 'Transaction % is unbalanced by % paise', NEW.transaction_id, imbalance
                            USING ERRCODE = 'check_violation';
                    END IF;

                    RETURN NULL;
                END;
                $$;

                CREATE CONSTRAINT TRIGGER entries_must_balance
                AFTER INSERT ON entries
                DEFERRABLE INITIALLY DEFERRED
                FOR EACH ROW EXECUTE FUNCTION ensure_transaction_balanced();

                CREATE FUNCTION reject_entry_change() RETURNS trigger
                LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'Ledger entries are immutable; post a reversal instead'
                        USING ERRCODE = 'restrict_violation';
                END;
                $$;

                CREATE TRIGGER entries_are_immutable
                BEFORE UPDATE OR DELETE ON entries
                FOR EACH ROW EXECUTE FUNCTION reject_entry_change();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER entries_are_immutable ON entries;
                DROP TRIGGER entries_must_balance ON entries;
                DROP FUNCTION reject_entry_change();
                DROP FUNCTION ensure_transaction_balanced();
                """);

            migrationBuilder.DropTable(
                name: "entries");

            migrationBuilder.DropTable(
                name: "holds");

            migrationBuilder.DropTable(
                name: "idempotency_records");

            migrationBuilder.DropTable(
                name: "outbox_messages");

            migrationBuilder.DropTable(
                name: "processed_messages");

            migrationBuilder.DropTable(
                name: "reconciliation_runs");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "accounts");
        }
    }
}
