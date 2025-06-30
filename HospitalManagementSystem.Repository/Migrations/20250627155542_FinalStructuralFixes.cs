using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalManagementSystem.Repository.Migrations
{
    /// <inheritdoc />
    public partial class FinalStructuralFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_BookedAppointments_PatientId",
                table: "Appointments");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Patient",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Patient",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "BookedAppointmentId",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_BookedAppointmentId",
                table: "Appointments",
                column: "BookedAppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_BookedAppointments_BookedAppointmentId",
                table: "Appointments",
                column: "BookedAppointmentId",
                principalTable: "BookedAppointments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Patient_PatientId",
                table: "Appointments",
                column: "PatientId",
                principalTable: "Patient",
                principalColumn: "PatientId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_BookedAppointments_BookedAppointmentId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Patient_PatientId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_BookedAppointmentId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Patient");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Patient");

            migrationBuilder.DropColumn(
                name: "BookedAppointmentId",
                table: "Appointments");

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_BookedAppointments_PatientId",
                table: "Appointments",
                column: "PatientId",
                principalTable: "BookedAppointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
