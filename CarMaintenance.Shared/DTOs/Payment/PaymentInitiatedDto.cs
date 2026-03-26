namespace CarMaintenance.Shared.DTOs.Payment
{
    public class PaymentInitiatedDto
    {
        // الـ Frontend يفتح الـ URL ده في IFrame أو نافذة جديدة
        public string IFrameUrl { get; set; } = null!;

        // الـ Token للي يحتاجه Frontend لو هيبني IFrame بنفسه
        public string PaymentToken { get; set; } = null!;
    }
}
