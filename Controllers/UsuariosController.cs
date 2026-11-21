using System.Security.Claims;
using BibliotecaMultimedia.Api.Data;
using BibliotecaMultimedia.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaMultimedia.Api.Controllers;

[ApiController, Route("api/usuarios")]
[Authorize(Policy = "UsuarioApi")]
public class UsuariosController(BibliotecaDbContext db) : ControllerBase
{
    [HttpGet("me"), ProducesResponseType<UsuarioSalidaDto>(200), ProducesResponseType(401)]
    public async Task<ActionResult<UsuarioSalidaDto>> Perfil()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var u = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id);
        return u is null ? Unauthorized() : Ok(new UsuarioSalidaDto(u.Id, u.Email!, u.FechaRegistroUtc));
    }
}
