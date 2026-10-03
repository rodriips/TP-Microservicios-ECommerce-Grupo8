using Microsoft.AspNetCore.Mvc;
using Products.API.Common;
using Products.API.DTOs;
using Products.API.Exceptions;
using Products.API.Services;

namespace Products.API.Controllers;

/// <summary>
/// Endpoints del catálogo de productos.
/// El controller no tiene try/catch: los errores los lanza el service
/// y los responden los IExceptionHandler.
/// </summary>
[ApiController]
[Route("api/products")]
[Produces("application/json")]
[Tags("Products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Lista los productos, con filtros opcionales por categoría y nombre.
    /// </summary>
    /// <param name="categoria">Categoría exacta (ej: Electrónica). Opcional.</param>
    /// <param name="nombre">Texto que debe contener el nombre (ej: notebook). Opcional.</param>
    /// <response code="200">Listado de productos.</response>
    /// <response code="500">PRD-005 - Error interno al procesar el producto.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<List<ProductResponse>> ObtenerTodos([FromQuery] string? categoria, [FromQuery] string? nombre)
    {
        List<ProductResponse> productos = _productService.ObtenerTodos(categoria, nombre);
        return Ok(productos);
    }

    /// <summary>
    /// Obtiene un producto por su Id.
    /// </summary>
    /// <param name="id">Id del producto (GUID).</param>
    /// <response code="200">Producto encontrado.</response>
    /// <response code="404">PRD-001 - Producto no encontrado.</response>
    /// <response code="500">PRD-005 - Error interno al procesar el producto.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<ProductResponse> ObtenerPorId(Guid id)
    {
        ProductResponse producto = _productService.ObtenerPorId(id);
        return Ok(producto);
    }

    /// <summary>
    /// Crea un nuevo producto.
    /// </summary>
    /// <remarks>
    /// No puede existir otro producto con el mismo nombre en la misma categoría (PRD-003).
    /// </remarks>
    /// <param name="request">Datos del producto.</param>
    /// <response code="201">Producto creado.</response>
    /// <response code="400">PRD-002 - Los datos del producto son inválidos.</response>
    /// <response code="409">PRD-003 - Ya existe un producto con ese nombre en la categoría.</response>
    /// <response code="500">PRD-005 - Error interno al procesar el producto.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<ProductResponse> Crear([FromBody] ProductRequest request)
    {
        ValidarDatos();
        ProductResponse creado = _productService.Crear(request);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = creado.Id }, creado);
    }

    /// <summary>
    /// Actualiza todos los datos de un producto existente.
    /// </summary>
    /// <param name="id">Id del producto (GUID).</param>
    /// <param name="request">Nuevos datos del producto.</param>
    /// <response code="200">Producto actualizado.</response>
    /// <response code="400">PRD-002 - Los datos del producto son inválidos.</response>
    /// <response code="404">PRD-001 - Producto no encontrado.</response>
    /// <response code="500">PRD-005 - Error interno al procesar el producto.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public ActionResult<ProductResponse> Actualizar(Guid id, [FromBody] ProductRequest request)
    {
        ValidarDatos();
        ProductResponse actualizado = _productService.Actualizar(id, request);
        return Ok(actualizado);
    }

    /// <summary>
    /// Elimina un producto.
    /// </summary>
    /// <remarks>
    /// Antes de eliminar consulta a Orders.API: si hay órdenes Pendiente o Confirmada
    /// con este producto, no se elimina (PRD-004).
    /// </remarks>
    /// <param name="id">Id del producto (GUID).</param>
    /// <response code="204">Producto eliminado.</response>
    /// <response code="404">PRD-001 - Producto no encontrado.</response>
    /// <response code="409">PRD-004 - El producto tiene órdenes activas y no puede eliminarse.</response>
    /// <response code="500">PRD-005 - Error interno al procesar el producto.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Eliminar(Guid id)
    {
        await _productService.EliminarAsync(id);
        return NoContent();
    }

    /// <summary>
    /// Revisa las Data Annotations del request (Required, Range, etc.).
    /// Si hay errores, los junta separados por "; " y lanza PRD-002.
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

        throw new ValidationException(ErrorCodes.PRD_002, string.Join("; ", errores));
    }
}
