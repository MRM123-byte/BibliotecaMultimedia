using BibliotecaMultimedia.Api.Models;

namespace BibliotecaMultimedia.Api.DTOs;

public static class MapeoDtos
{
    public static GeneroSalidaDto Salida(this Genero g) => new(g.GeneroId, g.Codigo, g.Nombre);
    public static ReferenciaSalidaDto Salida(this ReferenciaExterna r) => new(r.ReferenciaExternaId,r.Proveedor,r.TipoRecurso,r.IdExterno);
    public static ContenidoSalidaDto Salida(this Contenido c) => new(c.ContenidoId,c.Titulo,c.TipoContenido,c.EsAnime,
        c.Descripcion,c.ImagenUrl,c.FechaLanzamiento,c.Generos.Select(g => g.Genero.Salida()).ToList(),c.Referencias.Select(r => r.Salida()).ToList());
    public static BibliotecaSalidaDto Salida(this EntradaBiblioteca e) => new(e.EntradaBibliotecaId,e.ContenidoId,
        e.Contenido.Titulo,e.Contenido.TipoContenido,e.Contenido.EsAnime,e.Estado,e.Puntuacion,e.Resena,e.FechaAgregadoUtc,e.FechaModificacionUtc);
}
