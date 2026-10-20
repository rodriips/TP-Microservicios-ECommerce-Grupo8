using Orders.API.Models;

namespace Orders.API.Data;

/// <summary>
/// Almacenamiento provisorio en memoria. Cuando la cátedra entregue la
/// librería de persistencia real, solo hay que cambiar esta clase.
/// </summary>
public class OrderRepository
{
    private readonly List<Order> _ordenes = new();

    public List<Order> ObtenerTodos()
    {
        return _ordenes;
    }

    public Order? ObtenerPorId(Guid id)
    {
        foreach (var orden in _ordenes)
        {
            if (orden.Id == id)
                return orden;
        }
        return null;
    }

    public void Agregar(Order orden)
    {
        _ordenes.Add(orden);
    }
}
