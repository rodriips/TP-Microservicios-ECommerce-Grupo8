using System.Data;
using System.Globalization;
using Dapper;

namespace Notifications.API.Data;

/// <summary>
/// SQLite tampoco tiene un tipo fecha: la guardamos como texto con el formato
/// "o" (por ejemplo 2026-11-08T15:30:00.0000000Z). Así no se pierde la "Z"
/// que indica que la hora es UTC. Se registra una sola vez en Program.cs.
/// </summary>
public class FechaTypeHandler : SqlMapper.TypeHandler<DateTime>
{
    /// <summary>
    /// C# → base: la fecha se guarda como texto.
    /// </summary>
    public override void SetValue(IDbDataParameter parametro, DateTime valor)
    {
        parametro.Value = valor.ToString("o");
    }

    /// <summary>
    /// Base → C#: el texto se convierte otra vez en fecha (manteniendo que es UTC).
    /// </summary>
    public override DateTime Parse(object valor)
    {
        string texto = (string)valor;
        return DateTime.Parse(texto, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }
}
