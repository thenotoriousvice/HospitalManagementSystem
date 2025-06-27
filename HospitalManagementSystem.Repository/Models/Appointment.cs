using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace HospitalManagementSystem.Repository.Models
{
    public enum AppointmentStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled
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
        public BookedAppointment? Patient { get; set; }

        public int? DoctorId { get; set; }
        public Doctor? Doctor { get; set; }

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public string Reason { get; set; }

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        public DateTime RequestedAt { get; set; } = DateTime.Now;

        public DateTime? ApprovedRejectedAt { get; set; }
    }
}
