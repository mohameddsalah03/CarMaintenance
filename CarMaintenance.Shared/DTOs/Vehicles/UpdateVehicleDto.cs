using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace CarMaintenance.Shared.DTOs.Vehicles
{
    public class UpdateVehicleDto
    {
        [JsonIgnore]
        public int Id { get; set; }

        [Required(ErrorMessage = "موديل السيارة مطلوب")]
        [MaxLength(100)]
        public string Model { get; set; } = null!;

        [Required(ErrorMessage = "العلامة التجارية مطلوبة")]
        [MaxLength(50)]
        public string Brand { get; set; } = null!;

        [Required]
        [Range(1900, 2027, ErrorMessage = "السنة يجب أن تكون بين 1900 و 2027")]
        public int Year { get; set; }

        [Required(ErrorMessage = "رقم اللوحة مطلوب")]
        [MaxLength(20)]
        public string PlateNumber { get; set; } = null!;
    }
}