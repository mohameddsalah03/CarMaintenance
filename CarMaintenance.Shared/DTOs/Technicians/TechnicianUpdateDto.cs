using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarMaintenance.Shared.DTOs.Technicians
{
    public class TechnicianUpdateDto
    {
        [Required]
        public required string DisplayName { get; set; }

        [Required]
        public required string UserName { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        public required string PhoneNumber { get; set; }


        [Required]
        [MaxLength(200)]
        public required string Specialization { get; set; }
    }
}
