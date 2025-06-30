using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagementSystem.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddBillToAppointmentRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppointmentId",
                table: "Bills",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bills_AppointmentId",
                table: "Bills",
                column: "AppointmentId",
                unique: true,
                filter: "[AppointmentId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_Appointments_AppointmentId",
                table: "Bills",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_Appointments_AppointmentId",
                table: "Bills");

            migrationBuilder.DropIndex(
                name: "IX_Bills_AppointmentId",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "Bills");
        }
    }
}
