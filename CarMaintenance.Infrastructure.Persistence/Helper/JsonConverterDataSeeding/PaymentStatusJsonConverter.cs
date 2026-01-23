using System.Text.Json;
using System.Text.Json.Serialization;
using CarMaintenance.Core.Domain.Models.Data.Enums;

namespace CarMaintenance.Infrastructure.Persistence.Helper.JsonConverterDataSeeding
{
    public class PaymentStatusJsonConverter : JsonConverter<PaymentStatus>
    {
        public override PaymentStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var statusAsString = reader.GetString();
            return statusAsString?.ToLower() switch
            {
                "pending" => PaymentStatus.Pending,
                "paid" => PaymentStatus.Paid,
                "failed" => PaymentStatus.Failed,
                _ => PaymentStatus.Pending
            };
        }

        public override void Write(Utf8JsonWriter writer, PaymentStatus value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}