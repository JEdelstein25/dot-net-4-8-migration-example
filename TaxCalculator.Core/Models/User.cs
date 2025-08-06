using System;

namespace TaxCalculator.Core.Models
{
    public class User
    {
        public Guid UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string TFN { get; set; }
        public string ResidencyStatus { get; set; } = "Resident";
        public DateTime CreatedDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
