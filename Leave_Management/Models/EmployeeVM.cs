using System;
using System.ComponentModel;

namespace Leave_Management.Models
{
    public class EmployeeVM
    {
        public string Id { get; set; }
        [DisplayName("User Name")]
        public string UserName { get; set; }
        public string Email { get; set; }
        [DisplayName("Phone Number")]
        public string PhoneNumber { get; set; }
        [DisplayName("First Name")]
        public string Firstname { get; set; }
        [DisplayName("Last Name")]
        public string Lastname { get; set; }
        [DisplayName("Tax ID Number")]
        public string TaxId { get; set; }
        [DisplayName("Date Of Birth")]
        public DateTime DateOfBirth { get; set; }
        [DisplayName("Join Date")]
        public DateTime DateJoined { get; set; }
    }
}