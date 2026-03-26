using System.Text.Json;
using System.Text.Json.Serialization;
using CarMaintenance.Core.Domain.Models.Data.Enums;

namespace CarMaintenance.Infrastructure.Persistence.Helper.JsonConverterDataSeeding
{
    public class PaymentMethodJsonConverter : JsonConverter<PaymentMethod>
    {
        public override PaymentMethod Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var methodAsString = reader.GetString();
            return methodAsString?.ToLower() switch
            {
                "cash" => PaymentMethod.Cash,
                "creditcard" => PaymentMethod.CreditCard,
                _ => PaymentMethod.Cash
            };
        }

        public override void Write(Utf8JsonWriter writer, PaymentMethod value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}