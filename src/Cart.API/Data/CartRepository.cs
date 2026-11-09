using Cart.API.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace Cart.API.Data;

/// <summary>
/// Guarda los carritos en la base SQLite usando Dapper.
/// Usa dos tablas: Carts (una fila por carrito) y CartItems (una fila por producto del carrito).
/// Cada método abre su propia conexión y la cierra al terminar (el "using").
/// Las tablas las crea DatabaseInitializer al levantar el servicio.
/// (Mismo esquema que ProductRepository en Products.API.)
/// </summary>
public class CartRepository
{
    private readonly string _connectionString;

    public CartRepository(IConfiguration configuration)
    {
        // Se lee de appsettings.json ("ConnectionStrings:DefaultConnection").
        string? connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = "Data Source=cart.db";
        }
        _connectionString = connectionString;
    }

    /// <summary>
    /// Devuelve el carrito del usuario con sus ítems, o null si no tiene uno.
    /// </summary>
    public ShoppingCart? ObtenerPorUsuario(Guid usuarioId)
    {
        string sqlCarrito = @"SELECT UsuarioId, FechaActualizacion
                              FROM Carts
                              WHERE UsuarioId = @UsuarioId";

        string sqlItems = @"SELECT ProductoId, Cantidad
                            FROM CartItems
                            WHERE UsuarioId = @UsuarioId";

        DynamicParameters parametros = new DynamicParameters();
        parametros.Add("UsuarioId", usuarioId);

        using (SqliteConnection conexion = new SqliteConnection(_connectionString))
        {
            // 1. La fila del carrito (si no existe, el usuario no tiene carrito)
            ShoppingCart? carrito = conexion.QuerySingleOrDefault<ShoppingCart>(sqlCarrito, parametros);
            if (carrito == null)
            {
                return null;
            }

            // 2. Los ítems del carrito
            IEnumerable<CartItem> filas = conexion.Query<CartItem>(sqlItems, parametros);
            carrito.Items = new List<CartItem>(filas);
            return carrito;
        }
    }

    /// <summary>
    /// Guarda el carrito completo: si ya existía lo reemplaza, si no lo crea.
    /// Se borran los ítems anteriores y se insertan los de la lista actual.
    /// </summary>
    public void Guardar(ShoppingCart carrito)
    {
        string sqlCarrito = @"INSERT OR REPLACE INTO Carts (UsuarioId, FechaActualizacion)
                              VALUES (@UsuarioId, @FechaActualizacion)";

        string sqlBorrarItems = "DELETE FROM CartItems WHERE UsuarioId = @UsuarioId";

        string sqlItem = @"INSERT INTO CartItems (UsuarioId, ProductoId, Cantidad)
                           VALUES (@UsuarioId, @ProductoId, @Cantidad)";

        DynamicParameters parametrosCarrito = new DynamicParameters();
        parametrosCarrito.Add("UsuarioId", carrito.UsuarioId);
        parametrosCarrito.Add("FechaActualizacion", carrito.FechaActualizacion);

        using (SqliteConnection conexion = new SqliteConnection(_connectionString))
        {
            conexion.Execute(sqlCarrito, parametrosCarrito);
            conexion.Execute(sqlBorrarItems, parametrosCarrito);

            foreach (CartItem item in carrito.Items)
            {
                DynamicParameters parametrosItem = new DynamicParameters();
                parametrosItem.Add("UsuarioId", carrito.UsuarioId);
                parametrosItem.Add("ProductoId", item.ProductoId);
                parametrosItem.Add("Cantidad", item.Cantidad);
                conexion.Execute(sqlItem, parametrosItem);
            }
        }
    }

    /// <summary>
    /// Borra el carrito del usuario y todos sus ítems.
    /// </summary>
    public void Eliminar(Guid usuarioId)
    {
        string sqlItems = "DELETE FROM CartItems WHERE UsuarioId = @UsuarioId";
        string sqlCarrito = "DELETE FROM Carts WHERE UsuarioId = @UsuarioId";

        DynamicParameters parametros = new DynamicParameters();
        parametros.Add("UsuarioId", usuarioId);

        using (SqliteConnection conexion = new SqliteConnection(_connectionString))
        {
            conexion.Execute(sqlItems, parametros);
            conexion.Execute(sqlCarrito, parametros);
        }
    }
}
