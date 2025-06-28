using HospitalManagementSystem.BusinessLogic.Interfaces;
using HospitalManagementSystem.Repository.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks; // Needed for Task
using System; // Needed for Guid

namespace HospitalManagementSystem.Controllers // Changed namespace based on common ASP.NET Core project structure. If yours is different, please revert.
{
    public class BillingController : Controller
    {
        private readonly IBillService _billService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IAppointmentService _appointmentService; // Inject IAppointmentService

        public BillingController(IBillService billService, IWebHostEnvironment webHostEnvironment, IAppointmentService appointmentService)
        {
            _billService = billService;
            _webHostEnvironment = webHostEnvironment;
            _appointmentService = appointmentService; // Initialize IAppointmentService
        }

        [HttpGet]
        public IActionResult Index(int billId = 0, int? patientId = null, int? appointmentId = null) // Add patientId and appointmentId parameters
        {
            Bill bill = new Bill();
            ViewBag.Action = "Submit";
            ViewBag.AppointmentId = appointmentId; // Pass AppointmentId to the view

            if (billId > 0)
            {
                var existingBill = _billService.GetBillById(billId);
                if (existingBill != null)
                {
                    bill = existingBill;
                    ViewBag.Action = "Update";
                }
            }
            else if (patientId.HasValue) // Only pre-fill PatientId if creating a new bill and patientId is provided
            {
                bill.PatientId = patientId.Value;
            }

            return View(bill);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(Bill bill, IFormFile? uploadedFile, int? appointmentId) // Add appointmentId to POST
        {
            if (ModelState.IsValid)
            {
                // Handle file upload
                if (uploadedFile != null && uploadedFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + uploadedFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadedFile.CopyToAsync(fileStream);
                    }
                    bill.UploadedFilePath = "/uploads/" + uniqueFileName;
                }

                // Set initial status for a new bill
                if (bill.BillId == 0)
                {
                    bill.Status = BillStatus.PENDING_PAYMENT;
                }
                else
                {
                    // If updating an existing bill, retain its current status unless explicitly changed elsewhere
                    var existingBill = _billService.GetBillById(bill.BillId);
                    if (existingBill != null)
                    {
                        bill.Status = existingBill.Status;
                    }
                }

                _billService.SaveBill(bill);

                // Update the associated appointment status to PaymentPending
                if (appointmentId.HasValue && appointmentId.Value > 0)
                {
                    await _appointmentService.UpdateAppointmentStatusAsync(appointmentId.Value, AppointmentStatus.PaymentPending);
                }

                TempData["SuccessMessage"] = "Bill uploaded successfully and appointment status updated to Payment Pending.";
                // Redirect to the doctor's MyAppointments page
                return RedirectToAction("MyAppointments", "Doctor"); // Assuming "Doctor" is the controller for MyAppointments
            }

            // If model state is not valid, return to the form view
            ViewBag.AppointmentId = appointmentId;
            return View(bill);
        }

        // Removed Payment and ProcessPayment actions as per user request

        [HttpGet]
        public IActionResult GetBillDetails(int billId)
        {
            var bill = _billService.GetBillById(billId);
            if (bill == null)
            {
                return NotFound();
            }
            return Json(new
            {
                bill.BillId,
                bill.PatientId,
                bill.TotalAmount,
                bill.BillDate,
                bill.Status,
                bill.UploadedFilePath
            });
        }

        public IActionResult Show()
        {
            var data = _billService.GetAllBills();
            return View(data);
        }

        public IActionResult Delete(int billId)
        {
            _billService.DeleteBill(billId);
            return RedirectToAction("Show");
        }
    }
}