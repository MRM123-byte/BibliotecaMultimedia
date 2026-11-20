# Biblioteca Multimedia API

Proyecto final de Programación Web III. API para gestionar un catálogo de películas,
series y videojuegos y la biblioteca privada de cada usuario.

- Integrante: **Mauricio Rivero Mendez**.
- Usuario de GitHub: pendiente de indicar por el integrante.
- Repositorio Git: pendiente de creación o vinculación.
- Plataforma: **.NET 10**, ASP.NET Core, Entity Framework Core y SQL Server.

## Paso 1: estructura inicial

La solución está en `BibliotecaMultimedia.slnx` y la Web API en
`src/BibliotecaMultimedia.Api`. Se utilizarán controladores, modelos, DTO y una
carpeta de datos para mantener separadas las responsabilidades.

```powershell
dotnet restore
dotnet build
dotnet run --project src/BibliotecaMultimedia.Api --launch-profile http
```

Swagger local: http://localhost:5189/swagger

Los pasos de base de datos, migraciones y pruebas se documentarán al completar
las siguientes etapas. Las integraciones reales con TMDB y RAWG corresponden
a una etapa posterior; esta entrega se verificará con datos de prueba locales.
