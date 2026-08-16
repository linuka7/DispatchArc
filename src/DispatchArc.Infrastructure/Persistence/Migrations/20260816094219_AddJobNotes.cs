using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DispatchArc.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJobNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_notes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_job_notes_service_jobs_ServiceJobId",
                        column: x => x.ServiceJobId,
                        principalTable: "service_jobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_notes_tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_job_notes_users_AuthorUserId",
                        column: x => x.AuthorUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_notes_AuthorUserId",
                table: "job_notes",
                column: "AuthorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_job_notes_ServiceJobId",
                table: "job_notes",
                column: "ServiceJobId");

            migrationBuilder.CreateIndex(
                name: "IX_job_notes_TenantId_ServiceJobId_CreatedAtUtc",
                table: "job_notes",
                columns: new[] { "TenantId", "ServiceJobId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_notes");
        }
    }
}
