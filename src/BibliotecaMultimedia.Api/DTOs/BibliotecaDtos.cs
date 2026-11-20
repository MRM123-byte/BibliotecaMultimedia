using System.ComponentModel.DataAnnotations;
using BibliotecaMultimedia.Api.Models;

namespace BibliotecaMultimedia.Api.DTOs;

public class BibliotecaCrearDto
{
    [Range(1, int.MaxValue)] public int ContenidoId { get; set; }
}
public class BibliotecaActualizarDto
{
    [EnumDataType(typeof(EstadoConsumo))] public EstadoConsumo Estado { get; set; }
    [Range(1,10)] public byte? Puntuacion { get; set; }
    [StringLength(4000)] public string? Resena { get; set; }
}
public record BibliotecaSalidaDto(int EntradaBibliotecaId, int ContenidoId, string Titulo,
    TipoContenido TipoContenido, bool? EsAnime, EstadoConsumo Estado, byte? Puntuacion, string? Resena,
    DateTime FechaAgregadoUtc, DateTime FechaModificacionUtc);
public record UsuarioSalidaDto(string UsuarioId, string Email, DateTime FechaRegistroUtc);
