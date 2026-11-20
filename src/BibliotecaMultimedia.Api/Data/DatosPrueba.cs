using BibliotecaMultimedia.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BibliotecaMultimedia.Api.Data;

public static class DatosPrueba
{
    public static async Task CargarAsync(IServiceProvider services, IConfiguration config)
    {
        var password=config["Seed:Password"] ?? throw new InvalidOperationException("Configura Seed__Password con una contraseña de prueba antes de ejecutar --seed.");
        using var scope=services.CreateScope();
        var db=scope.ServiceProvider.GetRequiredService<BibliotecaDbContext>();
        var users=scope.ServiceProvider.GetRequiredService<UserManager<Usuario>>();
        var roles=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if(!await roles.RoleExistsAsync("Catalogador")) Comprobar(await roles.CreateAsync(new("Catalogador")));
        foreach(var email in new[]{"catalogador@example.test","lector@example.test"})
        {
            var user=await users.FindByEmailAsync(email);
            if(user is null)
            {
                user=new Usuario{ UserName=email,Email=email,EmailConfirmed=true };
                Comprobar(await users.CreateAsync(user,password));
            }
            if(email.StartsWith("catalogador") && !await users.IsInRoleAsync(user,"Catalogador"))
                Comprobar(await users.AddToRoleAsync(user,"Catalogador"));
        }
        foreach(var (code,name) in new[]{("AVENTURA","Aventura"),("ANIMACION","Animación"),("ESTRATEGIA","Estrategia")})
            if(!await db.Generos.AnyAsync(g=>g.Codigo==code)) db.Generos.Add(new(){Codigo=code,Nombre=name});
        await db.SaveChangesAsync();
        var generos=await db.Generos.ToDictionaryAsync(g=>g.Codigo);
        var ejemplos=new[]{
            ("Viaje al faro (demostración)",TipoContenido.Pelicula,false,"TMDB","movie","demo-pelicula","AVENTURA"),
            ("Guardianes del cielo (demostración)",TipoContenido.Serie,true,"TMDB","tv","demo-serie","ANIMACION"),
            ("Ciudad de papel (demostración)",TipoContenido.Videojuego,false,"RAWG","game","demo-juego","ESTRATEGIA")};
        foreach(var (titulo,tipo,anime,proveedor,recurso,id,genero) in ejemplos)
        {
            if(await db.ReferenciasExternas.AnyAsync(r=>r.Proveedor==proveedor && r.TipoRecurso==recurso && r.IdExterno==id)) continue;
            db.Contenidos.Add(new(){Titulo=titulo,TipoContenido=tipo,EsAnime=anime,
                Descripcion="Dato ficticio para pruebas académicas. No procede de una API externa.",
                Generos=[new(){GeneroId=generos[genero].GeneroId}],
                Referencias=[new(){Proveedor=proveedor,TipoRecurso=recurso,IdExterno=id}]});
        }
        await db.SaveChangesAsync();
    }
    private static void Comprobar(IdentityResult result)
    {
        if(!result.Succeeded) throw new InvalidOperationException(string.Join("; ",result.Errors.Select(e=>e.Description)));
    }
}
