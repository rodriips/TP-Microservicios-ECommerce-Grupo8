using Orders.API.DTOs;

namespace Orders.API.Services;

public interface IOrderService
{
    List<OrderResponse> ObtenerTodos(Guid? usuarioId);
    OrderResponse ObtenerPorId(Guid id);
    Task<OrderResponse> CrearAsync(CrearOrdenRequest request);
    CambiarEstadoResponse CambiarEstado(Guid id, CambiarEstadoRequest request);
}
