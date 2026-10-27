using Cart.API.DTOs;
using Cart.API.Services;
using Cart.API.Exceptions;
using Cart.API.Common;
using Microsoft.AspNetCore.Mvc;

namespace Cart.API.Controllers;

/// <summary>
/// Gestión del carrito de compras de cada usuario.
/// Todos los endpoints de escritura validan stock en tiempo real consultando Products.API.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;
    private readonly ILogger<CartController> _logger;

    public CartController(ICartService cartService, ILogger<CartController> logger)
    {
        _cartService = cartService;
        _logger = logger;
    }

    // ---------------------------------------------------------------
    // GET /api/cart/{userId}
    // ---------------------------------------------------------------

    /// <summary>
    /// Obtiene el carrito de compras del usuario.
    /// </summary>
    /// <param name="userId">ID del usuario (GUID).</param>
    /// <returns>Lista de ítems del carrito.</returns>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<CartItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult ObtenerCarrito(Guid userId)
    {
        var items = _cartService.ObtenerCarrito(userId);
        return Ok(items);
    }

    // ---------------------------------------------------------------
    // POST /api/cart/{userId}/items
    // ---------------------------------------------------------------

    /// <summary>
    /// Agrega un producto al carrito del usuario.
    /// Consulta Products.API para validar la existencia del producto (CRT-002) y su stock (CRT-003).
    /// Si el usuario no tenía carrito, lo crea automáticamente.
    /// </summary>
    /// <param name="userId">ID del usuario (GUID).</param>
    /// <param name="request">Datos del ítem a agregar.</param>
    [HttpPost("{userId:guid}/items")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AgregarItem(Guid userId, [FromBody] CartItemRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errores = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            throw new ValidationException(ErrorCodes.CRT_004, errores);
        }

        await _cartService.AgregarItemAsync(userId, request);
        return NoContent();
    }

    // ---------------------------------------------------------------
    // PUT /api/cart/{userId}/items/{productId}
    // ---------------------------------------------------------------

    /// <summary>
    /// Actualiza la cantidad de un producto en el carrito del usuario.
    /// Verifica stock actualizado en Products.API antes de confirmar el cambio (CRT-003).
    /// </summary>
    /// <param name="userId">ID del usuario (GUID).</param>
    /// <param name="productId">ID del producto a actualizar (GUID).</param>
    /// <param name="request">Nueva cantidad deseada.</param>
    [HttpPut("{userId:guid}/items/{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ActualizarItem(Guid userId, Guid productId, [FromBody] UpdateItemQuantityRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errores = string.Join("; ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            throw new ValidationException(ErrorCodes.CRT_004, errores);
        }

        await _cartService.ActualizarItemAsync(userId, productId, request);
        return NoContent();
    }

    // ---------------------------------------------------------------
    // DELETE /api/cart/{userId}/items/{productId}
    // ---------------------------------------------------------------

    /// <summary>
    /// Elimina un producto específico del carrito del usuario.
    /// </summary>
    /// <param name="userId">ID del usuario (GUID).</param>
    /// <param name="productId">ID del producto a eliminar (GUID).</param>
    [HttpDelete("{userId:guid}/items/{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult EliminarItem(Guid userId, Guid productId)
    {
        _cartService.EliminarItem(userId, productId);
        return NoContent();
    }

    // ---------------------------------------------------------------
    // DELETE /api/cart/{userId}
    // ---------------------------------------------------------------

    /// <summary>
    /// Vacía por completo el carrito del usuario.
    /// </summary>
    /// <param name="userId">ID del usuario (GUID).</param>
    [HttpDelete("{userId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public IActionResult VaciarCarrito(Guid userId)
    {
        _cartService.VaciarCarrito(userId);
        return NoContent();
    }
}
