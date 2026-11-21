using System.ComponentModel.DataAnnotations;
using BibliotecaMultimedia.Api.Data;
using BibliotecaMultimedia.Api.DTOs;
using BibliotecaMultimedia.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaMultimedia.Api.Controllers;

[ApiController]
[Route("api/contenidos")]
[Produces("application/json")]
public class ContenidosController(BibliotecaDbContext db) : ControllerBase
{
    private IQueryable<Contenido> Detalles() => db.Contenidos.AsSplitQuery().Include(c => c.Generos).ThenInclude(g => g.Genero).Include(c => c.Referencias);

    /// <summary>Lista el catálogo local con filtros y paginación.</summary>
    [HttpGet]
    [ProducesResponseType<PaginaDto<ContenidoSalidaDto>>(200)]
    [ProducesResponseType<ValidationProblemDetails>(400)]
    public async Task<ActionResult<PaginaDto<ContenidoSalidaDto>>> Listar([FromQuery, StringLength(100)] string? titulo,
        [FromQuery, EnumDataType(typeof(TipoContenido))] TipoContenido? tipo, [FromQuery] bool? anime,
        [FromQuery, Range(1, 1000000)] int pagina = 1, [FromQuery, Range(1, 100)] int tamano = 20)
    {
        var q = Detalles().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(titulo)) q = q.Where(c => c.Titulo.Contains(titulo.Trim()));
        if (tipo.HasValue) q = q.Where(c => c.TipoContenido == tipo);
        if (anime.HasValue) q = q.Where(c => c.EsAnime == anime);
        var total = await q.CountAsync();
        var items = await q.OrderBy(c => c.Titulo).ThenBy(c => c.ContenidoId).Skip((pagina - 1) * tamano).Take(tamano).ToListAsync();
        return Ok(new PaginaDto<ContenidoSalidaDto>(pagina, tamano, total, items.Select(c => c.Salida()).ToList()));
    }

    /// <summary>Consulta una ficha, sus géneros y referencias externas.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ContenidoSalidaDto>(200)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<ActionResult<ContenidoSalidaDto>> Obtener(int id)
    {
        var c = await Detalles().AsNoTracking().SingleOrDefaultAsync(c => c.ContenidoId == id);
        return c is null ? Problem(statusCode: 404, title: "Contenido no encontrado") : Ok(c.Salida());
    }

    /// <summary>Crea una ficha con sus géneros y una referencia. Solo Catalogador.</summary>
    [HttpPost]
    [Authorize(Policy = "UsuarioApi", Roles = "Catalogador")]
    [ProducesResponseType<ContenidoSalidaDto>(201)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<ActionResult<ContenidoSalidaDto>> Crear(ContenidoEntradaDto dto)
    {
        var error = await ValidarReferencias(dto);
        if (error is not null) return Problem(statusCode: 400, title: error);
        var c = new Contenido();
        Aplicar(c, dto);
        db.Contenidos.Add(c);
        await db.SaveChangesAsync();
        await db.Entry(c).Collection(x => x.Generos).Query().Include(x => x.Genero).LoadAsync();
        return CreatedAtAction(nameof(Obtener), new { id = c.ContenidoId }, c.Salida());
    }

    /// <summary>Reemplaza los datos editables de una ficha. Solo Catalogador.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "UsuarioApi", Roles = "Catalogador")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Actualizar(int id, ContenidoEntradaDto dto)
    {
        var c = await Detalles().SingleOrDefaultAsync(c => c.ContenidoId == id);
        if (c is null) return Problem(statusCode: 404, title: "Contenido no encontrado");
        var error = await ValidarReferencias(dto, id);
        if (error is not null) return Problem(statusCode: 400, title: error);
        Aplicar(c, dto);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Elimina una ficha únicamente si no está en ninguna biblioteca.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "UsuarioApi", Roles = "Catalogador")]
    [ProducesResponseType(204)]
    [ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> Eliminar(int id)
    {
        var c = await db.Contenidos.FindAsync(id);
        if (c is null) return Problem(statusCode: 404, title: "Contenido no encontrado");
        if (await db.EntradasBiblioteca.AnyAsync(e => e.ContenidoId == id))
            return Problem(statusCode: 400, title: "No se puede eliminar una ficha utilizada en bibliotecas");
        db.Contenidos.Remove(c);
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Consulta los géneros relacionados con una obra.</summary>
    [HttpGet("{id:int}/generos")]
    [ProducesResponseType<List<GeneroSalidaDto>>(200)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<ActionResult<List<GeneroSalidaDto>>> Generos(int id)
    {
        if (!await db.Contenidos.AnyAsync(c => c.ContenidoId == id)) return Problem(statusCode: 404, title: "Contenido no encontrado");
        return Ok(await db.ContenidoGeneros.Where(c => c.ContenidoId == id)
            .Select(c => new GeneroSalidaDto(c.GeneroId, c.Genero.Codigo, c.Genero.Nombre)).ToListAsync());
    }

    /// <summary>Consulta la identidad externa de una ficha. No llama al proveedor.</summary>
    [HttpGet("{id:int}/referencias")]
    [ProducesResponseType<List<ReferenciaSalidaDto>>(200)]
    [ProducesResponseType<ProblemDetails>(404)]
    public async Task<ActionResult<List<ReferenciaSalidaDto>>> Referencias(int id)
    {
        if (!await db.Contenidos.AnyAsync(c => c.ContenidoId == id)) return Problem(statusCode: 404, title: "Contenido no encontrado");
        return Ok(await db.ReferenciasExternas.Where(r => r.ContenidoId == id)
            .Select(r => new ReferenciaSalidaDto(r.ReferenciaExternaId, r.Proveedor, r.TipoRecurso, r.IdExterno)).ToListAsync());
    }

    private async Task<string?> ValidarReferencias(ContenidoEntradaDto d, int? actual = null)
    {
        if (await db.Generos.CountAsync(g => d.GeneroIds.Contains(g.GeneroId)) != d.GeneroIds.Count)
            return "Uno o más géneros no existen";
        if (await db.ReferenciasExternas.AnyAsync(r => r.ContenidoId != actual && r.Proveedor == d.Referencia.Proveedor
            && r.TipoRecurso == d.Referencia.TipoRecurso && r.IdExterno == d.Referencia.IdExterno))
            return "La referencia externa ya identifica otra obra";
        return null;
    }

    private static void Aplicar(Contenido c, ContenidoEntradaDto d)
    {
        c.Titulo = d.Titulo.Trim(); c.TipoContenido = d.TipoContenido; c.EsAnime = d.EsAnime;
        c.Descripcion = string.IsNullOrWhiteSpace(d.Descripcion) ? null : d.Descripcion.Trim();
        c.ImagenUrl = d.ImagenUrl; c.FechaLanzamiento = d.FechaLanzamiento; c.FechaSincronizacionUtc = DateTime.UtcNow;
        foreach (var g in c.Generos.Where(g => !d.GeneroIds.Contains(g.GeneroId)).ToList()) c.Generos.Remove(g);
        foreach (var id in d.GeneroIds.Where(id => c.Generos.All(g => g.GeneroId != id))) c.Generos.Add(new() { GeneroId = id });
        var r = c.Referencias.SingleOrDefault();
        if (r is null) { r = new(); c.Referencias.Add(r); }
        r.Proveedor = d.Referencia.Proveedor; r.TipoRecurso = d.Referencia.TipoRecurso; r.IdExterno = d.Referencia.IdExterno;
    }
}
