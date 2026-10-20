using Cart.API.Models;

namespace Cart.API.Data;

/// <summary>
/// Almacena los carritos en una lista en memoria.
/// PROVISORIO: la consigna dice que la persistencia la provee la cátedra como librería.
/// Cuando la tengamos, se cambia solo esta clase y el resto del código queda igual.
/// </summary>
public class CartRepository
{
    private readonly List<ShoppingCart> _carritos = new List<ShoppingCart>();

    /// <summary>
    /// Devuelve el carrito del usuario, o null si no tiene uno.
    /// </summary>
    public ShoppingCart? ObtenerPorUsuario(Guid usuarioId)
    {
        foreach (var carrito in _carritos)
        {
            if (carrito.UsuarioId == usuarioId)
            {
                return carrito;
            }
        }
        return null;
    }

    /// <summary>
    /// Agrega un carrito nuevo al repositorio.
    /// </summary>
    public void Agregar(ShoppingCart carrito)
    {
        _carritos.Add(carrito);
    }

    /// <summary>
    /// Elimina el carrito del repositorio.
    /// </summary>
    public void Eliminar(ShoppingCart carrito)
    {
        _carritos.Remove(carrito);
    }
}
