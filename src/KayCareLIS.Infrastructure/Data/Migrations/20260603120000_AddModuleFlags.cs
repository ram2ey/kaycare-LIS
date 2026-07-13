using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KayCareLIS.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModuleFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLaboratoryEnabled",
                table: "FacilitySettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsRadiologyEnabled",
                table: "FacilitySettings",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLaboratoryEnabled",
                table: "FacilitySettings");

            migrationBuilder.DropColumn(
                name: "IsRadiologyEnabled",
                table: "FacilitySettings");
        }
    }
}

