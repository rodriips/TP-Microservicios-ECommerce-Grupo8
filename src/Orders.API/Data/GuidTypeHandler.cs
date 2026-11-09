using System.Data;
using Dapper;

namespace Orders.API.Data;

/// <summary>
/// SQLite no tiene un tipo Guid: lo guardamos como texto.
/// Esta clase le explica a Dapper cómo convertirlo en las dos direcciones.
/// Se registra una sola vez en Program.cs.
/// </summary>
public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    /// <summary>
    /// C# → base: el Guid se guarda como texto.
    /// </summary>
    public override void SetValue(IDbDataParameter parametro, Guid valor)
    {
        parametro.Value = valor.ToString();
    }

    /// <summary>
    /// Base → C#: el texto se convierte otra vez en Guid.
    /// </summary>
    public override Guid Parse(object valor)
    {
        string texto = (string)valor;
        return Guid.Parse(texto);
    }
}
