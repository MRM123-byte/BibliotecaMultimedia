namespace BibliotecaMultimedia.Api.Models;

public enum EstadoConsumo { Pendiente = 1, EnCurso = 2, Completado = 3, EnPausa = 4, Abandonado = 5 }

public class EntradaBiblioteca
{
    public int EntradaBibliotecaId { get; set; }
    public string UsuarioId { get; set; } = "";
    public Usuario Usuario { get; set; } = null!;
    public int ContenidoId { get; set; }
    public Contenido Contenido { get; set; } = null!;
    public EstadoConsumo Estado { get; set; } = EstadoConsumo.Pendiente;
    public byte? Puntuacion { get; set; }
    public string? Resena { get; set; }
    public DateTime FechaAgregadoUtc { get; set; } = DateTime.UtcNow;
    public DateTime FechaModificacionUtc { get; set; } = DateTime.UtcNow;
}
