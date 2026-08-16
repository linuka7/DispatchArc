using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DispatchArc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenPaymentConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedReference",
                table: "payments",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                "UPDATE payments SET \"NormalizedReference\" = upper(btrim(\"Reference\")) WHERE \"Reference\" <> '';");
            migrationBuilder.CreateIndex(
                name: "IX_payments_TenantId_InvoiceId_NormalizedReference",
                table: "payments",
                columns: new[] { "TenantId", "InvoiceId", "NormalizedReference" },
                unique: true,
                filter: "\"NormalizedReference\" <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payments_TenantId_InvoiceId_NormalizedReference",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "NormalizedReference",
                table: "payments");
        }
    }
}
