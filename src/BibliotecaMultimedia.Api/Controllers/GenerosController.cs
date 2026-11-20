using BibliotecaMultimedia.Api.Data;
using BibliotecaMultimedia.Api.DTOs;
using BibliotecaMultimedia.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaMultimedia.Api.Controllers;

[ApiController, Route("api/generos")]
public class GenerosController(BibliotecaDbContext db) : ControllerBase
{
    /// <summary>Lista el vocabulario local de géneros.</summary>
    [HttpGet, ProducesResponseType<List<GeneroSalidaDto>>(200)]
    public async Task<ActionResult<List<GeneroSalidaDto>>> Listar() => Ok(await db.Generos.AsNoTracking()
        .OrderBy(g => g.Nombre).Select(g => new GeneroSalidaDto(g.GeneroId,g.Codigo,g.Nombre)).ToListAsync());

    /// <summary>Consulta un género por su identificador.</summary>
    [HttpGet("{id:int}"), ProducesResponseType<GeneroSalidaDto>(200), ProducesResponseType<ProblemDetails>(404)]
    public async Task<ActionResult<GeneroSalidaDto>> Obtener(int id)
    {
        var g=await db.Generos.FindAsync(id);
        return g is null ? Problem(statusCode:404,title:"Género no encontrado") : Ok(g.Salida());
    }

    /// <summary>Crea un género. Solo Catalogador.</summary>
    [HttpPost, Authorize(Policy="UsuarioApi",Roles="Catalogador")]
    [ProducesResponseType<GeneroSalidaDto>(201), ProducesResponseType<ProblemDetails>(400)]
    [ProducesResponseType(401), ProducesResponseType(403)]
    public async Task<ActionResult<GeneroSalidaDto>> Crear(GeneroEntradaDto dto)
    {
        if(await db.Generos.AnyAsync(g => g.Codigo==dto.Codigo)) return Problem(statusCode:400,title:"El código de género ya existe");
        var g=new Genero { Codigo=dto.Codigo,Nombre=dto.Nombre.Trim() };
        db.Generos.Add(g);await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Obtener),new { id=g.GeneroId },g.Salida());
    }

    /// <summary>Actualiza un género. Solo Catalogador.</summary>
    [HttpPut("{id:int}"), Authorize(Policy="UsuarioApi",Roles="Catalogador")]
    [ProducesResponseType(204), ProducesResponseType<ProblemDetails>(400), ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType(401), ProducesResponseType(403)]
    public async Task<IActionResult> Actualizar(int id,GeneroEntradaDto dto)
    {
        var g=await db.Generos.FindAsync(id);
        if(g is null) return Problem(statusCode:404,title:"Género no encontrado");
        if(await db.Generos.AnyAsync(g => g.GeneroId!=id && g.Codigo==dto.Codigo)) return Problem(statusCode:400,title:"El código de género ya existe");
        g.Nombre=dto.Nombre.Trim();g.Codigo=dto.Codigo;await db.SaveChangesAsync();return NoContent();
    }

    /// <summary>Elimina un género sin obras asociadas. Solo Catalogador.</summary>
    [HttpDelete("{id:int}"), Authorize(Policy="UsuarioApi",Roles="Catalogador")]
    [ProducesResponseType(204), ProducesResponseType<ProblemDetails>(400), ProducesResponseType<ProblemDetails>(404)]
    [ProducesResponseType(401), ProducesResponseType(403)]
    public async Task<IActionResult> Eliminar(int id)
    {
        var g=await db.Generos.FindAsync(id);
        if(g is null) return Problem(statusCode:404,title:"Género no encontrado");
        if(await db.ContenidoGeneros.AnyAsync(c => c.GeneroId==id)) return Problem(statusCode:400,title:"El género tiene obras asociadas");
        db.Generos.Remove(g);await db.SaveChangesAsync();return NoContent();
    }
}
