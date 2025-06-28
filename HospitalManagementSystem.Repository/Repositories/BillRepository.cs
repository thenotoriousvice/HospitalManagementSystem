namespace BillingAndPayments.Repository.Repositories
{
    using HospitalManagementSystem.Repository.Data;
    using HospitalManagementSystem.Repository.Interfaces;
    using HospitalManagementSystem.Repository.Models;
    using Microsoft.EntityFrameworkCore; // Add this using directive
    using System.Collections.Generic;
    using System.Linq;

    public class BillRepository : IBillRepository
    {
        private readonly ApplicationDbContext _context;

        public BillRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public Bill GetBillById(int billId)
        {
            return _context.Bills.Find(billId);
        }

        public List<Bill> GetAllBills()
        {
            return _context.Bills.ToList();
        }

        public void AddBill(Bill bill)
        {
            _context.Bills.Add(bill);
        }

        public void UpdateBill(Bill bill)
        {
            // Retrieve the existing entity from the database or the change tracker
            var existingBill = _context.Bills.Find(bill.BillId);
            if (existingBill != null)
            {
                // Update the properties of the existing entity with the new values
                _context.Entry(existingBill).CurrentValues.SetValues(bill);
            }
            else
            {
                // Optionally, handle the case where the bill to update is not found.
                // For an update operation, it's typically expected that the entity already exists.
                // You might choose to throw an exception or log an error here.
                throw new InvalidOperationException($"Bill with ID {bill.BillId} not found for update.");
            }
        }

        public void DeleteBill(int billId)
        {
            var bill = _context.Bills.Find(billId);
            if (bill != null)
            {
                _context.Bills.Remove(bill);
            }
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }
    }
}