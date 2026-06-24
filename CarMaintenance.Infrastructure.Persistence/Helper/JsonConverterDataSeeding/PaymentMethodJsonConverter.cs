using CarMaintenance.Core.Domain.Models.Data.Enums;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CarMaintenance.Infrastructure.Persistence.Helper.JsonConverterDataSeeding
{
    public class PaymentMethodJsonConverter : JsonConverter<PaymentMethod?>
    {
        public override PaymentMethod? Read(ref Utf8JsonReader reader,Type typeToConvert,JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            var methodAsString = reader.GetString();
            return methodAsString?.ToLower() switch
            {
                "cash" => PaymentMethod.Cash,
                "creditcard" => PaymentMethod.CreditCard,
                _ => null  
            };
        }

        public override void Write(Utf8JsonWriter writer, PaymentMethod? value,JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.ToString());
            else
                writer.WriteNullValue();
        }
    }
}