using System.ComponentModel.DataAnnotations;

namespace HospitalManagementSystem.Repository.Models
{
    public enum AppointmentStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled,
        Completed, 
        PaymentPending, 
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
        public string PatientName { get; set; }

        [Required]
        [EmailAddress]
        public string PatientEmail { get; set; } 
        [Required]
        [Phone]
        public string PatientPhoneNumber { get; set; } 

        public int? PatientId { get; set; }

        
        public Patient? Patient { get; set; } 

        public int? DoctorId { get; set; }
        public Doctor? Doctor { get; set; }

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public string Reason { get; set; }

        public AppointmentStatus Status { get; set; }

        public DateTime RequestedAt { get; set; }


        public DateTime? ApprovedRejectedAt { get; set; } 

       
        public Bill? Bill { get; set; }
    }
}