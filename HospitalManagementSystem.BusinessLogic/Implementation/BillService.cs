namespace HospitalManagementSystem.BusinessLogic.Services
{
    using HospitalManagementSystem.BusinessLogic.Interfaces;
    using HospitalManagementSystem.Repository.Interfaces;
    using HospitalManagementSystem.Repository.Models;
    using HospitalManagementSystem.Repository.Interfaces;
    using HospitalManagementSystem.Repository.Models;
    using System.Collections.Generic;

    public class BillService : IBillService
    {
        private readonly IBillRepository _billRepository;

        public BillService(IBillRepository billRepository)
        {
            _billRepository = billRepository;
        }

        public Bill GetBillById(int billId)
        {
            return _billRepository.GetBillById(billId);
        }

        public List<Bill> GetAllBills()
        {
            return _billRepository.GetAllBills();
        }

        public void SaveBill(Bill bill)
        {
            if (bill.BillId > 0)
            {
                _billRepository.UpdateBill(bill);
            }
            else
            {
                _billRepository.AddBill(bill);
            }
            _billRepository.SaveChanges();
        }

        public void DeleteBill(int billId)
        {
            _billRepository.DeleteBill(billId);
            _billRepository.SaveChanges();
        }

        public List<Bill> GetBillsByPatientId(int patientId)
        {
            // Assuming your IBillRepository (and underlying BillRepository) has a way to filter by PatientId
            // If not, we might need to modify IBillRepository and BillRepository as well.
            // For now, let's assume GetAllBills() returns all bills and we filter here.
            // A more efficient way would be to add GetBillsByPatientId to IBillRepository.
            return _billRepository.GetAllBills().Where(b => b.PatientId == patientId).ToList();
        }
    }
}