using Orders.API.Models;

namespace Orders.API.Data;

/// <summary>
/// Almacenamiento provisorio en memoria. Cuando la cátedra entregue la
/// librería de persistencia real, solo hay que cambiar esta clase.
/// </summary>
public class OrderRepository
{
    // Lista donde se guardan las órdenes mientras la aplicación está encendida
    private readonly List<Order> _ordenes = new List<Order>();

    // Devuelve todas las órdenes guardadas
    public List<Order> ObtenerTodos()
    {
        return _ordenes;
    }

    // Busca una orden por Id; devuelve null si no existe
    public Order? ObtenerPorId(Guid id)
    {
        foreach (Order orden in _ordenes)
        {
            if (orden.Id == id)
            {
                return orden;
            }
        }
        return null;
    }

    // Agrega una orden nueva a la lista
    public void Agregar(Order orden)
    {
        _ordenes.Add(orden);
    }
}
