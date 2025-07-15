using HospitalManagementSystem.BusinessLogic.Interfaces;
using HospitalManagementSystem.Repository.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks; 
using System;

namespace HospitalManagementSystem.Controllers
{
    public class BillingController : Controller
    {
        private readonly IBillService _billService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IAppointmentService _appointmentService; 

        public BillingController(IBillService billService, IWebHostEnvironment webHostEnvironment, IAppointmentService appointmentService)
        {
            _billService = billService;
            _webHostEnvironment = webHostEnvironment;
            _appointmentService = appointmentService; 
        }

        [HttpGet]
        public IActionResult Index(int billId = 0, int? patientId = null, int? appointmentId = null) // Add patientId and appointmentId parameters
        {
            Bill bill = new Bill();
            ViewBag.Action = "Submit";
            ViewBag.AppointmentId = appointmentId; 

            if (billId > 0)
            {
                var existingBill = _billService.GetBillById(billId);
                if (existingBill != null)
                {
                    bill = existingBill;
                    ViewBag.Action = "Update";
                }
            }
            else if (patientId.HasValue) 
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
               
                return RedirectToAction("MyAppointments", "Doctor"); 
            }

           
            ViewBag.AppointmentId = appointmentId;
            return View(bill);
        }

        

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