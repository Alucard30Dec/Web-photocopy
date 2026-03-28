using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PhotoCopyHub.Infrastructure.Data;

#nullable disable

namespace PhotoCopyHub.Infrastructure.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("202603240001_SecurityAndOperationsHardening")]
    public partial class SecurityAndOperationsHardening : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreviousHash",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecordHash",
                table: "AuditLogs",
                type: "TEXT",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConfirmedByOperatorId",
                table: "PrintJobs",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "PrintJobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssignedOperatorId",
                table: "PrintJobs",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastStatusNote",
                table: "PrintJobs",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PaidWalletTransactionId",
                table: "PrintJobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundedByUserId",
                table: "PrintJobs",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "PrintJobs",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundReason",
                table: "PrintJobs",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmitIdempotencyKey",
                table: "PrintJobs",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderIdempotencyKey",
                table: "ProductOrders",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessedByOperatorId",
                table: "ProductOrders",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                table: "ProductOrders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessNote",
                table: "ProductOrders",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OrderIdempotencyKey",
                table: "SupportServiceOrders",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessedByOperatorId",
                table: "SupportServiceOrders",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                table: "SupportServiceOrders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProcessNote",
                table: "SupportServiceOrders",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Channel",
                table: "TopUpRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "CreateIdempotencyKey",
                table: "TopUpRequests",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastReviewIdempotencyKey",
                table: "TopUpRequests",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresAdminApproval",
                table: "TopUpRequests",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SecondReviewedByAdminId",
                table: "TopUpRequests",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SecondReviewedAt",
                table: "TopUpRequests",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondReviewNote",
                table: "TopUpRequests",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "WalletTransactions",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductStockMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ActorUserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    MovementType = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityChanged = table.Column<int>(type: "INTEGER", nullable: false),
                    StockBefore = table.Column<int>(type: "INTEGER", nullable: false),
                    StockAfter = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductStockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductStockMovements_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductStockMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_RecordHash",
                table: "AuditLogs",
                column: "RecordHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrintJobs_AssignedOperatorId",
                table: "PrintJobs",
                column: "AssignedOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintJobs_ConfirmedByOperatorId",
                table: "PrintJobs",
                column: "ConfirmedByOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintJobs_RefundedByUserId",
                table: "PrintJobs",
                column: "RefundedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PrintJobs_UserId_SubmitIdempotencyKey",
                table: "PrintJobs",
                columns: new[] { "UserId", "SubmitIdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductOrders_ProcessedByOperatorId",
                table: "ProductOrders",
                column: "ProcessedByOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductOrders_UserId_OrderIdempotencyKey",
                table: "ProductOrders",
                columns: new[] { "UserId", "OrderIdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductStockMovements_ActorUserId",
                table: "ProductStockMovements",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductStockMovements_ProductId_CreatedAt",
                table: "ProductStockMovements",
                columns: new[] { "ProductId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SupportServiceOrders_ProcessedByOperatorId",
                table: "SupportServiceOrders",
                column: "ProcessedByOperatorId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportServiceOrders_UserId_OrderIdempotencyKey",
                table: "SupportServiceOrders",
                columns: new[] { "UserId", "OrderIdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TopUpRequests_SecondReviewedByAdminId",
                table: "TopUpRequests",
                column: "SecondReviewedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_TopUpRequests_UserId_CreateIdempotencyKey",
                table: "TopUpRequests",
                columns: new[] { "UserId", "CreateIdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_UserId_TransactionType_IdempotencyKey",
                table: "WalletTransactions",
                columns: new[] { "UserId", "TransactionType", "IdempotencyKey" },
                unique: true);

            if (!ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.AddForeignKey(
                    name: "FK_PrintJobs_AspNetUsers_AssignedOperatorId",
                    table: "PrintJobs",
                    column: "AssignedOperatorId",
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);

                migrationBuilder.AddForeignKey(
                    name: "FK_PrintJobs_AspNetUsers_ConfirmedByOperatorId",
                    table: "PrintJobs",
                    column: "ConfirmedByOperatorId",
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);

                migrationBuilder.AddForeignKey(
                    name: "FK_PrintJobs_AspNetUsers_RefundedByUserId",
                    table: "PrintJobs",
                    column: "RefundedByUserId",
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);

                migrationBuilder.AddForeignKey(
                    name: "FK_ProductOrders_AspNetUsers_ProcessedByOperatorId",
                    table: "ProductOrders",
                    column: "ProcessedByOperatorId",
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);

                migrationBuilder.AddForeignKey(
                    name: "FK_SupportServiceOrders_AspNetUsers_ProcessedByOperatorId",
                    table: "SupportServiceOrders",
                    column: "ProcessedByOperatorId",
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);

                migrationBuilder.AddForeignKey(
                    name: "FK_TopUpRequests_AspNetUsers_SecondReviewedByAdminId",
                    table: "TopUpRequests",
                    column: "SecondReviewedByAdminId",
                    principalTable: "AspNetUsers",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            if (!ActiveProvider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                migrationBuilder.DropForeignKey(
                    name: "FK_PrintJobs_AspNetUsers_AssignedOperatorId",
                    table: "PrintJobs");

                migrationBuilder.DropForeignKey(
                    name: "FK_PrintJobs_AspNetUsers_ConfirmedByOperatorId",
                    table: "PrintJobs");

                migrationBuilder.DropForeignKey(
                    name: "FK_PrintJobs_AspNetUsers_RefundedByUserId",
                    table: "PrintJobs");

                migrationBuilder.DropForeignKey(
                    name: "FK_ProductOrders_AspNetUsers_ProcessedByOperatorId",
                    table: "ProductOrders");

                migrationBuilder.DropForeignKey(
                    name: "FK_SupportServiceOrders_AspNetUsers_ProcessedByOperatorId",
                    table: "SupportServiceOrders");

                migrationBuilder.DropForeignKey(
                    name: "FK_TopUpRequests_AspNetUsers_SecondReviewedByAdminId",
                    table: "TopUpRequests");
            }

            migrationBuilder.DropTable(
                name: "ProductStockMovements");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_RecordHash",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_PrintJobs_AssignedOperatorId",
                table: "PrintJobs");

            migrationBuilder.DropIndex(
                name: "IX_PrintJobs_ConfirmedByOperatorId",
                table: "PrintJobs");

            migrationBuilder.DropIndex(
                name: "IX_PrintJobs_RefundedByUserId",
                table: "PrintJobs");

            migrationBuilder.DropIndex(
                name: "IX_PrintJobs_UserId_SubmitIdempotencyKey",
                table: "PrintJobs");

            migrationBuilder.DropIndex(
                name: "IX_ProductOrders_ProcessedByOperatorId",
                table: "ProductOrders");

            migrationBuilder.DropIndex(
                name: "IX_ProductOrders_UserId_OrderIdempotencyKey",
                table: "ProductOrders");

            migrationBuilder.DropIndex(
                name: "IX_SupportServiceOrders_ProcessedByOperatorId",
                table: "SupportServiceOrders");

            migrationBuilder.DropIndex(
                name: "IX_SupportServiceOrders_UserId_OrderIdempotencyKey",
                table: "SupportServiceOrders");

            migrationBuilder.DropIndex(
                name: "IX_TopUpRequests_SecondReviewedByAdminId",
                table: "TopUpRequests");

            migrationBuilder.DropIndex(
                name: "IX_TopUpRequests_UserId_CreateIdempotencyKey",
                table: "TopUpRequests");

            migrationBuilder.DropIndex(
                name: "IX_WalletTransactions_UserId_TransactionType_IdempotencyKey",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "PreviousHash",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "RecordHash",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "AssignedOperatorId",
                table: "PrintJobs");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "PrintJobs");

            migrationBuilder.DropColumn(
                name: "ConfirmedByOperatorId",
                table: "PrintJobs");

            migrationBuilder.DropColumn(
                name: "LastStatusNote",
                table: "PrintJobs");

            migrationBuilder.DropColumn(
                name: "PaidWalletTransactionId",
                table: "PrintJobs");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "PrintJobs");

            migrationBuilder.DropColumn(
                name: "RefundedByUserId",
                table: "PrintJobs");

            migrationBuilder.DropColumn(
                name: "RefundReason",
                table: "PrintJobs");

            migrationBuilder.DropColumn(
                name: "SubmitIdempotencyKey",
                table: "PrintJobs");

            migrationBuilder.DropColumn(
                name: "OrderIdempotencyKey",
                table: "ProductOrders");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "ProductOrders");

            migrationBuilder.DropColumn(
                name: "ProcessedByOperatorId",
                table: "ProductOrders");

            migrationBuilder.DropColumn(
                name: "ProcessNote",
                table: "ProductOrders");

            migrationBuilder.DropColumn(
                name: "OrderIdempotencyKey",
                table: "SupportServiceOrders");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "SupportServiceOrders");

            migrationBuilder.DropColumn(
                name: "ProcessedByOperatorId",
                table: "SupportServiceOrders");

            migrationBuilder.DropColumn(
                name: "ProcessNote",
                table: "SupportServiceOrders");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "TopUpRequests");

            migrationBuilder.DropColumn(
                name: "CreateIdempotencyKey",
                table: "TopUpRequests");

            migrationBuilder.DropColumn(
                name: "LastReviewIdempotencyKey",
                table: "TopUpRequests");

            migrationBuilder.DropColumn(
                name: "RequiresAdminApproval",
                table: "TopUpRequests");

            migrationBuilder.DropColumn(
                name: "SecondReviewedAt",
                table: "TopUpRequests");

            migrationBuilder.DropColumn(
                name: "SecondReviewedByAdminId",
                table: "TopUpRequests");

            migrationBuilder.DropColumn(
                name: "SecondReviewNote",
                table: "TopUpRequests");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "WalletTransactions");
        }
    }
}
