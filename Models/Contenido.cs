namespace BibliotecaMultimedia.Api.Models;

public enum TipoContenido { Pelicula = 1, Serie = 2, Videojuego = 3 }

public class Contenido
{
    public int ContenidoId { get; set; }
    public string Titulo { get; set; } = "";
    public TipoContenido TipoContenido { get; set; }
    public bool? EsAnime { get; set; }
    public string? Descripcion { get; set; }
    public string? ImagenUrl { get; set; }
    public DateOnly? FechaLanzamiento { get; set; }
    public DateTime FechaCreacionUtc { get; set; } = DateTime.UtcNow;
    public DateTime FechaSincronizacionUtc { get; set; } = DateTime.UtcNow;
    public ICollection<ContenidoGenero> Generos { get; set; } = [];
    public ICollection<ReferenciaExterna> Referencias { get; set; } = [];
    public ICollection<EntradaBiblioteca> Entradas { get; set; } = [];
}
