# Biblioteca Multimedia API

Proyecto final de Programación Web III. API para gestionar un catálogo de películas,
series y videojuegos y la biblioteca privada de cada usuario.

- Integrante: **Mauricio Rivero Mendez**.
- Usuario de GitHub: [MRM123-byte](https://github.com/MRM123-byte).
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

## Paso 2: SQL Server y migraciones

Instalar SQL Server (Developer o Express) y tener permisos para crear una base
de datos. SSMS es una herramienta de administración, no sustituye al servidor.
La conexión de ejemplo en appsettings.json usa autenticación de Windows:

```text
Server=localhost;Database=BibliotecaMultimediaDb;Trusted_Connection=True;TrustServerCertificate=True
```

Si se usa una instancia con nombre, sustituir localhost por localhost\\NOMBRE_INSTANCIA
en JSON (dos barras para representar una). No guardar contraseñas reales en Git.

```powershell
dotnet tool restore
dotnet ef database update --project src/BibliotecaMultimedia.Api
```

La migración Inicial está incluida. No es necesario crear otra al clonar el
proyecto. El modelo incluye las seis entidades del dominio y las tablas técnicas
de ASP.NET Core Identity.
