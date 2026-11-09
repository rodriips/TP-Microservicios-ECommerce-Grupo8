using Dapper;
using Microsoft.Data.Sqlite;
using Orders.API.Models;

namespace Orders.API.Data;

/// <summary>
/// Único lugar con SQL del servicio de órdenes. Guarda y lee de SQLite con Dapper.
/// Cada método abre y cierra su propia conexión.
/// </summary>
public class OrderRepository
{
    private readonly string _connectionString;

    public OrderRepository(IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Data Source=orders.db";
        }
        _connectionString = connectionString;
    }

    /// <summary>Devuelve todas las órdenes (con sus ítems), de la más vieja a la más nueva.</summary>
    public List<Order> ObtenerTodos()
    {
        List<Order> ordenes;

        using (SqliteConnection conexion = new SqliteConnection(_connectionString))
        {
            string sqlOrdenes = "SELECT * FROM Orders ORDER BY FechaCreacion";
            IEnumerable<Order> filas = conexion.Query<Order>(sqlOrdenes);
            ordenes = new List<Order>(filas);

            foreach (Order orden in ordenes)
            {
                orden.Items = ObtenerItems(conexion, orden.Id);
            }
        }

        return ordenes;
    }

    /// <summary>Busca una orden por Id (con sus ítems); devuelve null si no existe.</summary>
    public Order? ObtenerPorId(Guid id)
    {
        Order? orden;

        using (SqliteConnection conexion = new SqliteConnection(_connectionString))
        {
            string sql = "SELECT * FROM Orders WHERE Id = @Id";
            DynamicParameters parametros = new DynamicParameters();
            parametros.Add("Id", id);

            orden = conexion.QuerySingleOrDefault<Order>(sql, parametros);

            if (orden != null)
            {
                orden.Items = ObtenerItems(conexion, orden.Id);
            }
        }

        return orden;
    }

    /// <summary>Guarda una orden nueva junto con todos sus ítems.</summary>
    public void Agregar(Order orden)
    {
        using (SqliteConnection conexion = new SqliteConnection(_connectionString))
        {
            string sqlOrden = @"
                INSERT INTO Orders (Id, UsuarioId, Total, Estado, FechaCreacion, FechaActualizacion)
                VALUES (@Id, @UsuarioId, @Total, @Estado, @FechaCreacion, @FechaActualizacion)";
            conexion.Execute(sqlOrden, orden);

            string sqlItem = @"
                INSERT INTO OrderItems (OrderId, ProductoId, Cantidad, PrecioUnitario)
                VALUES (@OrderId, @ProductoId, @Cantidad, @PrecioUnitario)";

            foreach (OrderItem item in orden.Items)
            {
                DynamicParameters parametros = new DynamicParameters();
                parametros.Add("OrderId", orden.Id);
                parametros.Add("ProductoId", item.ProductoId);
                parametros.Add("Cantidad", item.Cantidad);
                parametros.Add("PrecioUnitario", item.PrecioUnitario);
                conexion.Execute(sqlItem, parametros);
            }
        }
    }

    /// <summary>Actualiza el estado y la fecha de actualización de una orden.</summary>
    public void ActualizarEstado(Order orden)
    {
        using (SqliteConnection conexion = new SqliteConnection(_connectionString))
        {
            string sql = @"
                UPDATE Orders
                SET Estado = @Estado, FechaActualizacion = @FechaActualizacion
                WHERE Id = @Id";
            conexion.Execute(sql, orden);
        }
    }

    // Lee los ítems de una orden usando una conexión que ya está abierta
    private List<OrderItem> ObtenerItems(SqliteConnection conexion, Guid ordenId)
    {
        string sql = @"
            SELECT ProductoId, Cantidad, PrecioUnitario
            FROM OrderItems
            WHERE OrderId = @OrderId
            ORDER BY Id";
        DynamicParameters parametros = new DynamicParameters();
        parametros.Add("OrderId", ordenId);

        IEnumerable<OrderItem> filas = conexion.Query<OrderItem>(sql, parametros);
        return new List<OrderItem>(filas);
    }
}
