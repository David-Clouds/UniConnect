using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniConnect.Migrations
{
    /// <inheritdoc />
    public partial class AmpliarComunidad : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescripcionLarga",
                table: "Comunidades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Duracion",
                table: "Comunidades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Frecuencia",
                table: "Comunidades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Impacto",
                table: "Comunidades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Miembros",
                table: "Comunidades",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Organizacion",
                table: "Comunidades",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Comunidades",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ubicacion",
                table: "Comunidades",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescripcionLarga",
                table: "Comunidades");

            migrationBuilder.DropColumn(
                name: "Duracion",
                table: "Comunidades");

            migrationBuilder.DropColumn(
                name: "Frecuencia",
                table: "Comunidades");

            migrationBuilder.DropColumn(
                name: "Impacto",
                table: "Comunidades");

            migrationBuilder.DropColumn(
                name: "Miembros",
                table: "Comunidades");

            migrationBuilder.DropColumn(
                name: "Organizacion",
                table: "Comunidades");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Comunidades");

            migrationBuilder.DropColumn(
                name: "Ubicacion",
                table: "Comunidades");
        }
    }
}
