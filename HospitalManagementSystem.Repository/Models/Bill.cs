using System;
using System.ComponentModel.DataAnnotations;

namespace HospitalManagementSystem.Repository.Models
{
    public class Bill
    {
        [Key]
        public int BillId { get; set; }
        public int PatientId { get; set; }
        public decimal TotalAmount { get; set; }
        public BillStatus Status { get; set; } // Changed from PaymentStatus to BillStatus
        public DateTime BillDate { get; set; }

        public string? UploadedFilePath { get; set; } // New property for uploaded document path

        public int? AppointmentId { get; set; } // Foreign key to Appointment
        public Appointment? Appointment { get; set; } 
    }

    public enum BillStatus // New enum to handle different bill states
    {
        PENDING_PAYMENT,
        PAID,
        PENDING_INSURANCE // Added for insurance payment scenario
    }


}