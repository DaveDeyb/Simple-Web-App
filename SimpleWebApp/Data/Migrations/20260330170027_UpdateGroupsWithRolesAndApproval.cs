using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimpleWebApp.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGroupsWithRolesAndApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InviteCode",
                table: "Groups",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "GroupMembers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "GroupMembers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedBy",
                table: "GroupMembers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "GroupMembers",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InviteCode",
                table: "Groups");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "GroupMembers");
        }
    }
}
