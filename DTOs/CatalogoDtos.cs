using System.ComponentModel.DataAnnotations;
using BibliotecaMultimedia.Api.Models;

namespace BibliotecaMultimedia.Api.DTOs;

public class ReferenciaEntradaDto
{
    [Required, RegularExpression("^(TMDB|RAWG)$")]
    public string Proveedor { get; set; } = "";
    [Required, RegularExpression("^(movie|tv|game)$")]
    public string TipoRecurso { get; set; } = "";
    [Required, StringLength(100), RegularExpression("^[A-Za-z0-9_-]+$")]
    public string IdExterno { get; set; } = "";
}

public class ContenidoEntradaDto : IValidatableObject
{
    [Required, StringLength(500)] public string Titulo { get; set; } = "";
    [EnumDataType(typeof(TipoContenido))] public TipoContenido TipoContenido { get; set; }
    public bool? EsAnime { get; set; }
    [StringLength(10000)] public string? Descripcion { get; set; }
    [StringLength(2048), Url] public string? ImagenUrl { get; set; }
    public DateOnly? FechaLanzamiento { get; set; }
    [Required, MaxLength(50)] public List<int> GeneroIds { get; set; } = [];
    [Required] public ReferenciaEntradaDto Referencia { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (TipoContenido == TipoContenido.Videojuego && EsAnime != false)
            yield return new("Un videojuego debe tener esAnime=false.", [nameof(EsAnime)]);
        if (GeneroIds is not null && (GeneroIds.Any(id => id <= 0) || GeneroIds.Distinct().Count() != GeneroIds.Count))
            yield return new("Los identificadores de género deben ser positivos y no repetirse.", [nameof(GeneroIds)]);
        if (ImagenUrl is not null && (!Uri.TryCreate(ImagenUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https"))
            yield return new("La imagen debe tener una URL HTTPS.", [nameof(ImagenUrl)]);
        if (Referencia is not null && !ReferenciaCompatible(TipoContenido, Referencia.Proveedor, Referencia.TipoRecurso))
            yield return new("El proveedor y recurso no corresponden al tipo de contenido.", [nameof(Referencia)]);
    }

    public static bool ReferenciaCompatible(TipoContenido tipo, string proveedor, string recurso) =>
        (tipo, proveedor, recurso) is (TipoContenido.Pelicula, "TMDB", "movie")
        or (TipoContenido.Serie, "TMDB", "tv") or (TipoContenido.Videojuego, "RAWG", "game");
}

public class GeneroEntradaDto
{
    [Required, StringLength(50), RegularExpression("^[A-Z][A-Z0-9_]*$")]
    public string Codigo { get; set; } = "";
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
}

public record GeneroSalidaDto(int GeneroId, string Codigo, string Nombre);
public record ReferenciaSalidaDto(int ReferenciaExternaId, string Proveedor, string TipoRecurso, string IdExterno);
public record ContenidoSalidaDto(int ContenidoId, string Titulo, TipoContenido TipoContenido, bool? EsAnime,
    string? Descripcion, string? ImagenUrl, DateOnly? FechaLanzamiento,
    IReadOnlyList<GeneroSalidaDto> Generos, IReadOnlyList<ReferenciaSalidaDto> Referencias);
public record PaginaDto<T>(int Pagina, int Tamano, int Total, IReadOnlyList<T> Elementos);
