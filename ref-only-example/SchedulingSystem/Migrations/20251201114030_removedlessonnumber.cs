using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchedulingSystem.Migrations
{
    /// <inheritdoc />
    public partial class removedlessonnumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Lessons_LessonNumber",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "LessonNumber",
                table: "Lessons");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LessonNumber",
                table: "Lessons",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_LessonNumber",
                table: "Lessons",
                column: "LessonNumber");
        }
    }
}
