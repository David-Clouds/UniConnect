using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniConnect.Migrations
{
    /// <inheritdoc />
    public partial class PermisosOrganizadores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstudianteId",
                table: "Organizadores",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsAdministradorGeneral",
                table: "Estudiantes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Organizadores_EstudianteId",
                table: "Organizadores",
                column: "EstudianteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizadores_Estudiantes_EstudianteId",
                table: "Organizadores",
                column: "EstudianteId",
                principalTable: "Estudiantes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizadores_Estudiantes_EstudianteId",
                table: "Organizadores");

            migrationBuilder.DropIndex(
                name: "IX_Organizadores_EstudianteId",
                table: "Organizadores");

            migrationBuilder.DropColumn(
                name: "EstudianteId",
                table: "Organizadores");

            migrationBuilder.DropColumn(
                name: "EsAdministradorGeneral",
                table: "Estudiantes");
        }
    }
}
