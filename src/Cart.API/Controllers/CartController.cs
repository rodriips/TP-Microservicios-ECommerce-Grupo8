using Cart.API.Common;
using Cart.API.DTOs;
using Cart.API.Exceptions;
using Cart.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cart.API.Controllers;

/// <summary>
/// Gestión del carrito de compras de cada usuario.
/// Los endpoints de escritura validan stock en tiempo real consultando Products.API.
/// El controller no tiene try/catch: los errores los lanza el service
/// y los responden los IExceptionHandler.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Tags("Cart")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    /// <summary>
    /// Obtiene el carrito de compras del usuario.
    /// </summary>
    /// <param name="userId">ID del usuario (GUID).</param>
    /// <response code="200">Carrito del usuario con sus ítems.</response>
    /// <response code="404">CRT-001 - El usuario no tiene un carrito activo.</response>
    /// <response code="500">CRT-005 - Error interno al procesar el carrito.</response>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<CartResponse> ObtenerCarrito(Guid userId)
    {
        CartResponse carrito = _cartService.ObtenerCarrito(userId);
        return Ok(carrito);
    }

    /// <summary>
    /// Agrega un producto al carrito del usuario.
    /// </summary>
    /// <remarks>
    /// Consulta Products.API para validar que el producto exista (CRT-002) y tenga stock (CRT-003).
    /// Si el usuario no tenía carrito, lo crea automáticamente.
    /// </remarks>
    /// <param name="userId">ID del usuario (GUID).</param>
    /// <param name="request">Datos del ítem a agregar.</param>
    /// <response code="200">Ítem agregado. Devuelve el carrito actualizado.</response>
    /// <response code="400">CRT-004 - La cantidad es inválida (debe ser mayor a 0).</response>
    /// <response code="404">CRT-002 - El producto no existe en el catálogo.</response>
    /// <response code="422">CRT-003 - Stock insuficiente para el producto.</response>
    /// <response code="500">CRT-005 - Error interno al procesar el carrito.</response>
    [HttpPost("{userId}/items")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CartResponse>> AgregarItem(Guid userId, [FromBody] CartItemRequest request)
    {
        ValidarDatos();
        CartResponse carrito = await _cartService.AgregarItemAsync(userId, request);
        return Ok(carrito);
    }

    /// <summary>
    /// Actualiza la cantidad de un producto en el carrito del usuario.
    /// </summary>
    /// <remarks>
    /// Verifica el stock actualizado en Products.API antes de confirmar el cambio (CRT-003).
    /// </remarks>
    /// <param name="userId">ID del usuario (GUID).</param>
    /// <param name="productId">ID del producto a actualizar (GUID).</param>
    /// <param name="request">Nueva cantidad deseada.</param>
    /// <response code="200">Cantidad actualizada. Devuelve el carrito actualizado.</response>
    /// <response code="400">CRT-004 - La cantidad es inválida (debe ser mayor a 0).</response>
    /// <response code="404">CRT-001 - El usuario no tiene carrito, o CRT-002 - el producto no está en el carrito.</response>
    /// <response code="422">CRT-003 - Stock insuficiente para el producto.</response>
    /// <response code="500">CRT-005 - Error interno al procesar el carrito.</response>
    [HttpPut("{userId}/items/{productId}")]
    [ProducesResponseType(typeof(CartResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CartResponse>> ActualizarItem(Guid userId, Guid productId, [FromBody] UpdateItemQuantityRequest request)
    {
        ValidarDatos();
        CartResponse carrito = await _cartService.ActualizarItemAsync(userId, productId, request);
        return Ok(carrito);
    }

    /// <summary>
    /// Elimina un producto específico del carrito del usuario.
    /// </summary>
    /// <param name="userId">ID del usuario (GUID).</param>
    /// <param name="productId">ID del producto a eliminar (GUID).</param>
    /// <response code="204">Producto eliminado del carrito.</response>
    /// <response code="404">CRT-001 - El usuario no tiene carrito, o CRT-002 - el producto no está en el carrito.</response>
    /// <response code="500">CRT-005 - Error interno al procesar el carrito.</response>
    [HttpDelete("{userId}/items/{productId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult EliminarItem(Guid userId, Guid productId)
    {
        _cartService.EliminarItem(userId, productId);
        return NoContent();
    }

    /// <summary>
    /// Vacía por completo el carrito del usuario.
    /// </summary>
    /// <param name="userId">ID del usuario (GUID).</param>
    /// <response code="204">Carrito vaciado.</response>
    /// <response code="404">CRT-001 - El usuario no tiene un carrito activo.</response>
    /// <response code="500">CRT-005 - Error interno al procesar el carrito.</response>
    [HttpDelete("{userId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult VaciarCarrito(Guid userId)
    {
        _cartService.VaciarCarrito(userId);
        return NoContent();
    }

    /// <summary>
    /// Revisa las Data Annotations del request (Required, Range, etc.).
    /// Si hay errores, los junta separados por "; " y lanza CRT-004.
    /// </summary>
    private void ValidarDatos()
    {
        if (ModelState.IsValid)
        {
            return;
        }

        var errores = new List<string>();
        foreach (var campo in ModelState.Values)
        {
            foreach (var error in campo.Errors)
            {
                errores.Add(error.ErrorMessage);
            }
        }

        throw new ValidationException(ErrorCodes.CRT_004, string.Join("; ", errores));
    }
}
