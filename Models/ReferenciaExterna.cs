namespace BibliotecaMultimedia.Api.Models;

public class ReferenciaExterna
{
    public int ReferenciaExternaId { get; set; }
    public int ContenidoId { get; set; }
    public Contenido Contenido { get; set; } = null!;
    public string Proveedor { get; set; } = "";
    public string TipoRecurso { get; set; } = "";
    public string IdExterno { get; set; } = "";
}
