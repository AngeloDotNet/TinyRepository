using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TinyRepository.Sample.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationAuthorSampleEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SampleEntities_Authors_AuthorId",
                table: "SampleEntities");

            migrationBuilder.AddForeignKey(
                name: "FK_SampleEntities_Authors_AuthorId",
                table: "SampleEntities",
                column: "AuthorId",
                principalTable: "Authors",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SampleEntities_Authors_AuthorId",
                table: "SampleEntities");

            migrationBuilder.AddForeignKey(
                name: "FK_SampleEntities_Authors_AuthorId",
                table: "SampleEntities",
                column: "AuthorId",
                principalTable: "Authors",
                principalColumn: "Id");
        }
    }
}
