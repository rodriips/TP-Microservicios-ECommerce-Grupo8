using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Orders.API.Common;
using Orders.API.DTOs;
using Orders.API.Exceptions;
using Orders.API.Services;

namespace Orders.API.Controllers;

/// <summary>
/// Endpoints de órdenes de compra. No tiene try/catch: los errores los lanza
/// el service (o ValidarDatos acá abajo) y los responden los IExceptionHandler.
/// </summary>
[ApiController]
[Route("api/orders")]
[Produces("application/json")]
[Tags("Orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>Lista las órdenes, opcionalmente filtradas por usuario.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<List<OrderResponse>> ObtenerTodos([FromQuery] Guid? usuarioId)
    {
        return Ok(_orderService.ObtenerTodos(usuarioId));
    }

    /// <summary>Obtiene el detalle de una orden por su Id.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<OrderResponse> ObtenerPorId(Guid id)
    {
        return Ok(_orderService.ObtenerPorId(id));
    }

    /// <summary>
    /// Crea una nueva orden. Valida que el usuario exista (ORD-003), que cada
    /// producto exista (ORD-004) y que haya stock suficiente (ORD-005).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), 422)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderResponse>> Crear([FromBody] CrearOrdenRequest request)
    {
        ValidarDatos();
        var response = await _orderService.CrearAsync(request);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = response.Id }, response);
    }

    /// <summary>
    /// Cambia el estado de una orden. Valida que la transición sea lógica
    /// (Pendiente -&gt; Confirmada -&gt; Enviada -&gt; Entregada, o Cancelada); si no, ORD-006.
    /// </summary>
    [HttpPut("{id}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<CambiarEstadoResponse> CambiarEstado(Guid id, [FromBody] CambiarEstadoRequest request)
    {
        ValidarDatos();
        return Ok(_orderService.CambiarEstado(id, request));
    }

    private void ValidarDatos()
    {
        if (ModelState.IsValid)
            return;

        var mensajes = ModelState
            .SelectMany(kvp => kvp.Value?.Errors ?? Enumerable.Empty<Microsoft.AspNetCore.Mvc.ModelBinding.ModelError>())
            .Select(e => e.ErrorMessage);

        throw new ValidationException(ErrorCodes.ORD_002, string.Join("; ", mensajes));
    }
}
