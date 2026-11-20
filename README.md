# Biblioteca Multimedia API

Proyecto final de Programación Web III: catálogo compartido y biblioteca personal.

- **Integrante:** Mauricio Rivero Mendez.
- **GitHub:** [MRM123-byte](https://github.com/MRM123-byte).
- **Repositorio Git:** [MRM123-byte/BibliotecaMultimedia](https://github.com/MRM123-byte/BibliotecaMultimedia).
- **Versión:** .NET 10 (SDK 10.0.400 utilizado), ASP.NET Core 10 y EF Core 10.0.15.
- **Persistencia:** SQL Server. **Swagger:** Swashbuckle.AspNetCore 10.2.3.

> El repositorio es público y conserva el historial de commits por etapas para
> la revisión del profesor. El ZIP incluye el proyecto, las migraciones y el PDF
> de evidencias; el historial se consulta directamente en GitHub.

## 1. Preparar el equipo

Instalar el SDK .NET 10 y SQL Server Developer o Express. SSMS es opcional para
inspeccionar las tablas; no sustituye al motor SQL Server. Se requiere acceso a
NuGet y permiso para crear la base local. Ejecutar desde la raíz del proyecto:

```powershell
dotnet restore
dotnet tool restore
dotnet build
```

## 2. Conexión local a SQL Server

`src/BibliotecaMultimedia.Api/appsettings.json` contiene un ejemplo sin credenciales reales:

```text
Server=localhost;Database=BibliotecaMultimediaDb;Trusted_Connection=True;TrustServerCertificate=True
```

Usa autenticación de Windows. Para una instancia con nombre, dentro de JSON
puede usarse `Server=localhost\\SQLEXPRESS` (dos barras representan una).
También se puede configurar la conexión solo para la terminal actual:

```powershell
$env:ConnectionStrings__Biblioteca='Server=localhost;Database=BibliotecaMultimediaDb;Trusted_Connection=True;TrustServerCertificate=True'
```

No guardar contraseñas reales en Git. TrustServerCertificate se utiliza para
el desarrollo local; producción necesita un certificado válido. Esta conexión
no modifica las bases de otros proyectos.

## 3. Aplicar migraciones

```powershell
dotnet ef database update --project src/BibliotecaMultimedia.Api
```

La migración Inicial está incluida en `Data/Migrations`. No hace falta crear
otra al clonar. El comando crea la base y aplica las migraciones pendientes.
No usamos EnsureCreated ni borramos la base al iniciar. El esquema contiene
las seis entidades del dominio y las tablas técnicas de ASP.NET Core Identity.

## 4. Cargar datos de prueba

```powershell
$env:Seed__Password='Demo2026!Biblioteca'
dotnet run --project src/BibliotecaMultimedia.Api --no-build --launch-profile http -- --seed
```

La contraseña es un ejemplo público para cuentas ficticias locales, no una
credencial real. Puedes sustituirla antes de la primera carga.

| Cuenta ficticia | Permiso |
|---|---|
| catalogador@example.test | CRUD del catálogo y biblioteca propia |
| lector@example.test | Biblioteca propia y lectura del catálogo |

Se crean géneros y tres obras ficticias. Sus referencias demo-* no corresponden
a registros reales de proveedores. Repetir la carga no duplica sus registros ni
restablece las contraseñas existentes. Solo funciona en Development.

## 5. Abrir Swagger

```powershell
dotnet run --project src/BibliotecaMultimedia.Api --no-build --launch-profile http
```

Abrir **http://localhost:5189/swagger**. HTTP es solo para pruebas locales.
El perfil https ofrece https://localhost:7183/swagger y puede requerir
`dotnet dev-certs https --trust` para confiar en el certificado local.

1. Abrir `POST /api/auth/login`, pulsar Try it out y dejar useCookies=false.
2. Enviar el correo de prueba y la contraseña definida en el paso anterior.
3. Copiar accessToken y pegarlo en Authorize, sin escribir Bearer delante.
4. Ejecutar los endpoints. Para cambiar de usuario, retirar la autorización,
   iniciar sesión con la otra cuenta y pegar su token.

Identity emite tokens opacos, no JWT. Los controladores privados aceptan Bearer.
Los GET del catálogo son públicos aunque Swagger permita adjuntarles un token.
Retirar el token de Swagger no lo revoca en el servidor: el cierre de sesión
completo del cliente final queda para otra etapa. Las rutas auxiliares de
Identity para correo/2FA aparecen, pero no se configuró envío de correo ni
se cuentan estas rutas como cobertura evaluada.

## 6. Demostración paso a paso

1. GET /api/generos: consultar un generoId existente.
2. POST /api/contenidos como Catalogador con el ejemplo siguiente.
3. Comprobar 201 y la cabecera Location; consultar el ID creado mediante GET.
4. Ejecutar PUT y verificar sus cambios; consultar /{id}/generos y /{id}/referencias.
5. Añadir la obra con POST /api/biblioteca; modificar estado, nota y reseña.
6. Enviar una nota 11: 400. Consultar un ID inexistente: 404.
7. Detener la API con Ctrl+C, iniciarla de nuevo y consultar el mismo ID: 200.
8. Retirar la entrada personal y luego eliminar la ficha: ambos DELETE devuelven 204.

Ejemplo POST/PUT de contenido; adaptar generoIds a la base local:

```json
{
  "titulo": "Película de demostración",
  "tipoContenido": "Pelicula",
  "esAnime": false,
  "descripcion": "Ficha ficticia para la evaluación.",
  "imagenUrl": null,
  "fechaLanzamiento": "2026-01-10",
  "generoIds": [1],
  "referencia": { "proveedor": "TMDB", "tipoRecurso": "movie", "idExterno": "demo-evaluacion-001" }
}
```

Otra obra necesita otra referencia externa, no otro título. Para videojuegos:
Videojuego, esAnime=false, RAWG y game. Para series: Serie, TMDB y tv.
POST de biblioteca: `{"contenidoId":1}`. PUT de biblioteca:

```json
{"estado":"EnCurso","puntuacion":8,"resena":"Reseña privada de prueba."}
```

Estados: Pendiente, EnCurso, Completado, EnPausa, Abandonado. NULL retira la nota
o reseña. Watchlist: GET /api/biblioteca?estado=Pendiente. PUT reemplaza campos
editables, no es una modificación parcial. El propietario procede del token.

## 7. Evidencias

`docs/Evidencias.pdf` contiene las capturas de Swagger, los resultados HTTP y
la comprobación de persistencia después de reiniciar la aplicación.
Para repetir las pruebas, seguir el recorrido de la sección 6 desde Swagger.

## 8. Organización y alcance

- Models: entidades y enumeraciones.
- Data: contexto, migración y carga de prueba.
- DTOs: contratos y mapeos de entrada/salida.
- Controllers: rutas, autorización y reglas de esta etapa.
- Infrastructure: manejo común de errores de persistencia.
- docs/Evidencias.pdf: resultados de las pruebas de entrega.

Las seis entidades y todas las relaciones están representadas. No significa que
el producto final esté completo. Las reglas se extraerán a servicios reutilizables
cuando añadamos MVC u otro cliente. Por ahora la organización responde a la
rúbrica sin añadir capas innecesarias. TMDB/RAWG reales, importación externa,
clasificación automática de anime, frontend y Android quedan para después.
No se necesitan claves de API en esta entrega.

## 9. Git y ZIP

Los commits están separados por etapas y asociados a Mauricio Rivero Mendez
mediante la dirección noreply de MRM123-byte. Revisar con git log --oneline y
git show. El estudiante debe revisar, comprender y poder explicar los cambios
realizados con asistencia.

El repositorio público está disponible en
[GitHub](https://github.com/MRM123-byte/BibliotecaMultimedia), con el historial
completo en la rama `main`. Para obtener una copia:

```powershell
git clone https://github.com/MRM123-byte/BibliotecaMultimedia.git
cd BibliotecaMultimedia
```

Después de guardar nuevos cambios en Git, actualizar el remoto con `git push`.
Para generar un ZIP de los archivos del último commit, ejecutar desde la raíz:

```powershell
git archive --format=zip --prefix=BibliotecaMultimedia/ --output=BibliotecaMultimedia-entrega.zip HEAD
```

El ZIP incluye los archivos versionados, el PDF y las migraciones.
Las carpetas bin, obj, .vs, .git y los temporales no se incluyen.
La base se reconstruye con migraciones y datos de prueba, no mediante un .bak.

## Referencias técnicas

- [Migraciones de EF Core](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Aplicar migraciones](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying)
- [Swagger en ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/tutorials/web-api-help-pages-using-swagger?view=aspnetcore-10.0)
