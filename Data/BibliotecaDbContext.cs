using BibliotecaMultimedia.Api.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaMultimedia.Api.Data;

public class BibliotecaDbContext(DbContextOptions<BibliotecaDbContext> options)
    : IdentityDbContext<Usuario>(options)
{
    public DbSet<Contenido> Contenidos => Set<Contenido>();
    public DbSet<Genero> Generos => Set<Genero>();
    public DbSet<ContenidoGenero> ContenidoGeneros => Set<ContenidoGenero>();
    public DbSet<ReferenciaExterna> ReferenciasExternas => Set<ReferenciaExterna>();
    public DbSet<EntradaBiblioteca> EntradasBiblioteca => Set<EntradaBiblioteca>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<Usuario>(e =>
        {
            e.Property(u => u.Email).IsRequired();
            e.Property(u => u.NormalizedEmail).IsRequired();
            e.HasIndex(u => u.NormalizedEmail).IsUnique();
        });
        model.Entity<Contenido>(e =>
        {
            e.Property(c => c.Titulo).HasMaxLength(500).IsRequired();
            e.Property(c => c.ImagenUrl).HasMaxLength(2048);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Contenido_Tipo", "[TipoContenido] IN (1,2,3)");
                t.HasCheckConstraint("CK_Contenido_Anime", "[TipoContenido] <> 3 OR ([EsAnime] IS NOT NULL AND [EsAnime] = 0)");
                t.HasCheckConstraint("CK_Contenido_Titulo", "LEN(LTRIM(RTRIM([Titulo]))) > 0");
            });
        });
        model.Entity<Genero>(e =>
        {
            e.Property(g => g.Codigo).HasMaxLength(50).IsUnicode(false).IsRequired();
            e.Property(g => g.Nombre).HasMaxLength(100).IsRequired();
            e.HasIndex(g => g.Codigo).IsUnique();
        });
        model.Entity<ContenidoGenero>(e =>
        {
            e.HasKey(cg => new { cg.ContenidoId, cg.GeneroId });
            e.HasOne(cg => cg.Contenido).WithMany(c => c.Generos).HasForeignKey(cg => cg.ContenidoId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(cg => cg.Genero).WithMany(g => g.Contenidos).HasForeignKey(cg => cg.GeneroId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<ReferenciaExterna>(e =>
        {
            e.Property(r => r.Proveedor).HasMaxLength(20).IsUnicode(false).IsRequired();
            e.Property(r => r.TipoRecurso).HasMaxLength(20).IsUnicode(false).IsRequired();
            e.Property(r => r.IdExterno).HasMaxLength(100).IsUnicode(false).IsRequired();
            e.HasIndex(r => new { r.Proveedor, r.TipoRecurso, r.IdExterno }).IsUnique();
            e.HasIndex(r => new { r.ContenidoId, r.Proveedor, r.TipoRecurso }).IsUnique();
            e.HasOne(r => r.Contenido).WithMany(c => c.Referencias).HasForeignKey(r => r.ContenidoId).OnDelete(DeleteBehavior.Cascade);
            e.ToTable(t => t.HasCheckConstraint("CK_Referencia_ProveedorTipo", "([Proveedor] = 'TMDB' AND [TipoRecurso] IN ('movie','tv')) OR ([Proveedor] = 'RAWG' AND [TipoRecurso] = 'game')"));
        });
        model.Entity<EntradaBiblioteca>(e =>
        {
            e.Property(b => b.Resena).HasMaxLength(4000);
            e.HasIndex(b => new { b.UsuarioId, b.ContenidoId }).IsUnique();
            e.HasOne(b => b.Usuario).WithMany(u => u.Biblioteca).HasForeignKey(b => b.UsuarioId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(b => b.Contenido).WithMany(c => c.Entradas).HasForeignKey(b => b.ContenidoId).OnDelete(DeleteBehavior.Restrict);
            e.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Entrada_Puntuacion", "[Puntuacion] IS NULL OR [Puntuacion] BETWEEN 1 AND 10");
                t.HasCheckConstraint("CK_Entrada_Estado", "[Estado] IN (1,2,3,4,5)");
            });
        });
    }
}
