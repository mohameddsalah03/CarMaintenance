using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarMaintenance.Shared.DTOs.Technicians
{
    public class TechniciansDto
    {
        public string Id { get; set; } = null!;
        public string Specialization { get; set; } = null!;
        public decimal Rating { get; set; }
        public bool IsAvailable { get; set; }
        public string UserId { get; set; } = null!;

        // معلومات الـ User
        public string UserName { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
    }
}