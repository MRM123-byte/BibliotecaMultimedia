namespace BibliotecaMultimedia.Api.Models;

public class Genero
{
    public int GeneroId { get; set; }
    public string Codigo { get; set; } = "";
    public string Nombre { get; set; } = "";
    public ICollection<ContenidoGenero> Contenidos { get; set; } = [];
}
