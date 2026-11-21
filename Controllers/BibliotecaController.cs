using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BibliotecaMultimedia.Api.Data;
using BibliotecaMultimedia.Api.DTOs;
using BibliotecaMultimedia.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaMultimedia.Api.Controllers;

[ApiController, Route("api/biblioteca")]
[Authorize(Policy = "UsuarioApi")]
[ProducesResponseType(401)]
public class BibliotecaController(BibliotecaDbContext db) : ControllerBase
{
    private string UsuarioId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;
    private IQueryable<EntradaBiblioteca> Propias() => db.EntradasBiblioteca.Include(e => e.Contenido).Where(e => e.UsuarioId == UsuarioId);

    [HttpGet, ProducesResponseType<PaginaDto<BibliotecaSalidaDto>>(200), ProducesResponseType<ValidationProblemDetails>(400)]
    public async Task<ActionResult<PaginaDto<BibliotecaSalidaDto>>> Listar(
        [FromQuery, EnumDataType(typeof(EstadoConsumo))] EstadoConsumo? estado,
        [FromQuery, EnumDataType(typeof(TipoContenido))] TipoContenido? tipo,
        [FromQuery] bool? anime, [FromQuery, Range(1, int.MaxValue)] int? generoId, [FromQuery, StringLength(100)] string? titulo,
        [FromQuery, RegularExpression("^(titulo|fecha|puntuacion)$")] string orden = "fecha",
        [FromQuery, Range(1, 1000000)] int pagina = 1, [FromQuery, Range(1, 100)] int tamano = 20)
    {
        var q = Propias().AsNoTracking();
        if (estado.HasValue) q = q.Where(e => e.Estado == estado);
        if (tipo.HasValue) q = q.Where(e => e.Contenido.TipoContenido == tipo);
        if (anime.HasValue) q = q.Where(e => e.Contenido.EsAnime == anime);
        if (generoId.HasValue) q = q.Where(e => e.Contenido.Generos.Any(g => g.GeneroId == generoId));
        if (!string.IsNullOrWhiteSpace(titulo)) q = q.Where(e => e.Contenido.Titulo.Contains(titulo.Trim()));
        var total = await q.CountAsync();
        var ordered = orden switch
        {
            "titulo" => q.OrderBy(e => e.Contenido.Titulo).ThenBy(e => e.EntradaBibliotecaId),
            "puntuacion" => q.OrderByDescending(e => e.Puntuacion.HasValue).ThenByDescending(e => e.Puntuacion).ThenBy(e => e.EntradaBibliotecaId),
            _ => q.OrderByDescending(e => e.FechaAgregadoUtc).ThenBy(e => e.EntradaBibliotecaId)
        };
        var items = await ordered.Skip((pagina - 1) * tamano).Take(tamano).ToListAsync();
        return Ok(new PaginaDto<BibliotecaSalidaDto>(pagina, tamano, total, items.Select(e => e.Salida()).ToList()));
    }

    [HttpGet("{id:int}"), ProducesResponseType<BibliotecaSalidaDto>(200), ProducesResponseType<ProblemDetails>(404)]
    public async Task<ActionResult<BibliotecaSalidaDto>> Obtener(int id)
    {
        var e = await Propias().AsNoTracking().SingleOrDefaultAsync(e => e.EntradaBibliotecaId == id);
        return e is null ? Problem(statusCode: 404, title: "Entrada no encontrada") : Ok(e.Salida());
    }

    [HttpPost, ProducesResponseType<BibliotecaSalidaDto>(201), ProducesResponseType<ProblemDetails>(400)]
    public async Task<ActionResult<BibliotecaSalidaDto>> Crear(BibliotecaCrearDto dto)
    {
        var c = await db.Contenidos.FindAsync(dto.ContenidoId);
        if (c is null) return Problem(statusCode: 400, title: "El contenido indicado no existe");
        if (await Propias().AnyAsync(e => e.ContenidoId == dto.ContenidoId)) return Problem(statusCode: 400, title: "La obra ya está en tu biblioteca");
        var e = new EntradaBiblioteca { UsuarioId = UsuarioId, Contenido = c };
        db.EntradasBiblioteca.Add(e); await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obtener), new { id = e.EntradaBibliotecaId }, e.Salida());
    }

    [HttpPut("{id:int}"), ProducesResponseType(204), ProducesResponseType<ProblemDetails>(400), ProducesResponseType<ProblemDetails>(404)]
    public async Task<IActionResult> Actualizar(int id, BibliotecaActualizarDto dto)
    {
        var e = await Propias().SingleOrDefaultAsync(e => e.EntradaBibliotecaId == id);
        if (e is null) return Problem(statusCode: 404, title: "Entrada no encontrada");
        e.Estado = dto.Estado; e.Puntuacion = dto.Puntuacion;
        e.Resena = string.IsNullOrWhiteSpace(dto.Resena) ? null : dto.Resena.Trim();
        e.FechaModificacionUtc = DateTime.UtcNow; await db.SaveChangesAsync(); return NoContent();
    }

    [HttpDelete("{id:int}"), ProducesResponseType(204), ProducesResponseType<ProblemDetails>(404)]
    public async Task<IActionResult> Eliminar(int id)
    {
        var e = await Propias().SingleOrDefaultAsync(e => e.EntradaBibliotecaId == id);
        if (e is null) return Problem(statusCode: 404, title: "Entrada no encontrada");
        db.EntradasBiblioteca.Remove(e); await db.SaveChangesAsync(); return NoContent();
    }
}
