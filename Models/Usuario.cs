using Microsoft.AspNetCore.Identity;

namespace BibliotecaMultimedia.Api.Models;

public class Usuario : IdentityUser
{
    public DateTime FechaRegistroUtc { get; set; } = DateTime.UtcNow;
    public ICollection<EntradaBiblioteca> Biblioteca { get; set; } = [];
}
