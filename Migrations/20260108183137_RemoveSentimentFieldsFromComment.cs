using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicroSocialPlatform.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSentimentFieldsFromComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Date",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "SentimentAnalyzedAt",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "SentimentConfidence",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "SentimentLabel",
                table: "Comments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "Date",
                table: "Comments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "SentimentAnalyzedAt",
                table: "Comments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SentimentConfidence",
                table: "Comments",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SentimentLabel",
                table: "Comments",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
