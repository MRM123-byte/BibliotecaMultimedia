namespace BibliotecaMultimedia.Api.Models;

public class ContenidoGenero
{
    public int ContenidoId { get; set; }
    public Contenido Contenido { get; set; } = null!;
    public int GeneroId { get; set; }
    public Genero Genero { get; set; } = null!;
}
