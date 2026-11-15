using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ColetaSeletiva.Infrastructure.Persistencia.Conversores;

public sealed class UtcDateTimeOffsetConverter : ValueConverter<DateTimeOffset, DateTimeOffset>
{
    public UtcDateTimeOffsetConverter()
        : base(
            valor => valor.ToUniversalTime(),
            valor => valor.ToUniversalTime())
    {
    }
}
