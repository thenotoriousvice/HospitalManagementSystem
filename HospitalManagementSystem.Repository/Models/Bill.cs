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
        public BillStatus Status { get; set; } 
        public DateTime BillDate { get; set; }

        public string? UploadedFilePath { get; set; } 

        public int? AppointmentId { get; set; } 
        public Appointment? Appointment { get; set; } 
    }

    public enum BillStatus 
    {
        PENDING_PAYMENT,
        PAID,
        PENDING_INSURANCE 
    }


}