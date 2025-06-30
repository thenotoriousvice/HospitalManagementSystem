using System.ComponentModel.DataAnnotations;

namespace HospitalManagementSystem.Repository.Models
{
    public enum AppointmentStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled,
        Completed, // Ensure 'Completed' is present as recommended
        PaymentPending, // New status: Bill has been uploaded, awaiting patient payment
        PaymentCompleted
    }

    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime AppointmentDate { get; set; }

        [DataType(DataType.Time)]
        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        public TimeSpan? AppointmentTime { get; set; }

        [Required]
        [StringLength(100)]
        public string PatientName { get; set; } // Consider removing if always populated from Patient.Name

        [Required]
        [EmailAddress]
        public string PatientEmail { get; set; } // Consider removing if always populated from Patient.Email

        [Required]
        [Phone]
        public string PatientPhoneNumber { get; set; } // Consider removing if always populated from Patient.PhoneNumber

        public int? PatientId { get; set; }

        // CRITICAL FIX: This MUST be Patient? not BookedAppointment?
        public Patient? Patient { get; set; } // <--- CORRECTED THIS LINE

        public int? DoctorId { get; set; }
        public Doctor? Doctor { get; set; }

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public string Reason { get; set; }

        public AppointmentStatus Status { get; set; }

        public DateTime RequestedAt { get; set; }

        // NEW FIX: Add this property as it's missing but referenced in your code
        public DateTime? ApprovedRejectedAt { get; set; } // <--- ADD THIS LINE (make it nullable if not always set)

        // --- NEW: Add navigation property for Bill ---
        public Bill? Bill { get; set; }
    }
}