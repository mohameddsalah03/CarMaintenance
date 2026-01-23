using System.Text.Json;
using System.Text.Json.Serialization;
using CarMaintenance.Core.Domain.Models.Data.Enums;

namespace CarMaintenance.Infrastructure.Persistence.Helper.JsonConverterDataSeeding
{
    public class BookingStatusJsonConverter : JsonConverter<BookingStatus>
    {
        public override BookingStatus Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var statusAsString = reader.GetString();
            return statusAsString?.ToLower() switch
            {
                "pending" => BookingStatus.Pending,
                "inprogress" => BookingStatus.InProgress,
                "completed" => BookingStatus.Completed,
                "cancelled" => BookingStatus.Cancelled,
                _ => BookingStatus.Pending
            };
        }

        public override void Write(Utf8JsonWriter writer, BookingStatus value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}