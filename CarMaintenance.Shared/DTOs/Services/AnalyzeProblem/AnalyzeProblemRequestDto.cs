using System.ComponentModel.DataAnnotations;

namespace CarMaintenance.Shared.DTOs.Services.AnalyzeProblem
{
   
    public class AnalyzeProblemRequestDto
    {
        [Required(ErrorMessage = "وصف المشكلة مطلوب")]
        [MaxLength(500, ErrorMessage = "الوصف يجب ألا يتجاوز 500 حرف")]
        [MinLength(3, ErrorMessage = "الوصف يجب أن يكون أكثر من 3 أحرف")]
        public string ProblemDescription { get; set; } = null!;

        // Optional: if the user has a registered vehicle selected in the UI
        public int? VehicleId { get; set; }
    }
}