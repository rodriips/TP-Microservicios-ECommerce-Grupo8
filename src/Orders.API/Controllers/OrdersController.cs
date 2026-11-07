using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
    /// <param name="usuarioId">Opcional. Si se envía, devuelve solo las órdenes de ese usuario.</param>
    /// <response code="200">Lista de órdenes (puede estar vacía).</response>
    /// <response code="500">ORD-007: error interno del servidor.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<OrderResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<List<OrderResponse>> ObtenerTodos([FromQuery] Guid? usuarioId)
    {
        return Ok(_orderService.ObtenerTodos(usuarioId));
    }

    /// <summary>Obtiene el detalle de una orden por su Id.</summary>
    /// <param name="id">Id de la orden.</param>
    /// <response code="200">Orden encontrada.</response>
    /// <response code="404">ORD-001: la orden no existe.</response>
    /// <response code="500">ORD-007: error interno del servidor.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<OrderResponse> ObtenerPorId(Guid id)
    {
        return Ok(_orderService.ObtenerPorId(id));
    }

    /// <summary>
    /// Crea una nueva orden. Valida que el usuario exista (ORD-003), que cada
    /// producto exista (ORD-004) y que haya stock suficiente (ORD-005).
    /// Si un producto se repite en los items, sus cantidades se suman.
    /// </summary>
    /// <param name="request">Usuario y lista de productos con cantidad.</param>
    /// <response code="201">Orden creada con estado Pendiente.</response>
    /// <response code="400">ORD-002: datos inválidos (falta usuarioId, items vacío o cantidad menor a 1).</response>
    /// <response code="404">ORD-003: el usuario no existe. ORD-004: algún producto no existe.</response>
    /// <response code="422">ORD-005: stock insuficiente.</response>
    /// <response code="500">ORD-007: error interno del servidor (por ejemplo, Users.API o Products.API caídos).</response>
    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), 422)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderResponse>> Crear([FromBody] CrearOrdenRequest request)
    {
        ValidarDatos(request);
        OrderResponse response = await _orderService.CrearAsync(request);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = response.Id }, response);
    }

    /// <summary>
    /// Cambia el estado de una orden. Valida que la transición sea lógica
    /// (Pendiente -&gt; Confirmada -&gt; Enviada -&gt; Entregada, o Cancelada); si no, ORD-006.
    /// El estado se acepta sin importar mayúsculas y se guarda con la mayúscula correcta.
    /// </summary>
    /// <param name="id">Id de la orden.</param>
    /// <param name="request">Nuevo estado.</param>
    /// <response code="200">Estado actualizado.</response>
    /// <response code="400">ORD-002: falta el estado o no es uno de los valores permitidos.</response>
    /// <response code="404">ORD-001: la orden no existe.</response>
    /// <response code="409">ORD-006: la transición de estado no está permitida.</response>
    /// <response code="500">ORD-007: error interno del servidor.</response>
    [HttpPut("{id}/status")]
    [ProducesResponseType(typeof(CambiarEstadoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<CambiarEstadoResponse> CambiarEstado(Guid id, [FromBody] CambiarEstadoRequest request)
    {
        ValidarDatos();
        return Ok(_orderService.CambiarEstado(id, request));
    }

    // Revisa los errores de validación automática ([Required], [Range], JSON mal escrito).
    // Si hay alguno, los junta en un solo mensaje y lanza ORD-002 (400).
    private void ValidarDatos()
    {
        if (ModelState.IsValid)
        {
            return;
        }

        List<string> mensajes = new List<string>();

        foreach (KeyValuePair<string, ModelStateEntry> campo in ModelState)
        {
            if (campo.Value == null)
            {
                continue;
            }

            foreach (ModelError error in campo.Value.Errors)
            {
                mensajes.Add(error.ErrorMessage);
            }
        }

        throw new ValidationException(ErrorCodes.ORD_002, string.Join("; ", mensajes));
    }

    // Validación para crear una orden: primero las automáticas y después las que hay que hacer a mano.
    private void ValidarDatos(CrearOrdenRequest? request)
    {
        // Errores automáticos (items vacío, cantidad menor a 1, JSON mal escrito, etc.)
        ValidarDatos();

        // Si el JSON llegó vacío, request es null
        if (request == null)
        {
            throw new ValidationException(ErrorCodes.ORD_002, "El cuerpo de la solicitud es obligatorio.");
        }

        // Si no mandaron usuarioId, C# lo deja en 00000000-0000-0000-0000-000000000000 (Guid.Empty)
        if (request.UsuarioId == Guid.Empty)
        {
            throw new ValidationException(ErrorCodes.ORD_002, "El usuarioId es obligatorio.");
        }
    }
}
