using HospitalManagementSystem.Repository.Models;


namespace HospitalManagementSystem.BusinessLogic.Interfaces
{

    using System.Collections.Generic;

    public interface IBillService
    {
        Bill GetBillById(int billId);
        List<Bill> GetAllBills();
        void SaveBill(Bill bill);
        void DeleteBill(int billId);
        List<Bill> GetBillsByPatientId(int patientId);
    }
}