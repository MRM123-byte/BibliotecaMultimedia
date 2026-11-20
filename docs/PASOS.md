# Guía para explicar el desarrollo

## 1. Estructura

Creamos una Web API con controladores sobre .NET 10. Swagger proporciona una
interfaz para ejecutar peticiones HTTP; no es el frontend final de la plataforma.
El proyecto compila antes de comenzar el modelo. `bin`, `obj` y `.vs` no se
versionan porque contienen archivos generados o configuración del IDE.

## 2. Modelo y persistencia

Las seis entidades del diagrama se representan mediante clases. `Usuario` hereda
de IdentityUser: Identity se ocupa de contraseñas y tokens, y nuestro modelo añade
la relación con la biblioteca. Sus tablas técnicas no son nuevas entidades del dominio.

`BibliotecaDbContext` traduce las clases a tablas. Configura las claves compuestas
de ContenidoGenero, los índices únicos y las claves foráneas. Un índice único
usuario-contenido evita duplicados incluso si llegan dos solicitudes simultáneas.
La nota admite NULL y valores de 1 a 10; no se usa cero como ausencia de nota.

La migración es código versionado que describe cómo crear o modificar el esquema.
`dotnet ef database update` la aplica a SQL Server. No usamos EnsureCreated, porque
no sustituye al historial de migraciones solicitado en la consigna.

La conexión de ejemplo usa autenticación de Windows, no contiene contraseñas y
crea una base nueva llamada BibliotecaMultimediaDb. TrustServerCertificate es
una facilidad para el entorno local, no una configuración de producción.

## Decisiones para esta entrega

Se habilitará mantenimiento del catálogo solo para el rol Catalogador, sin panel
de administración. Es una adaptación al CRUD evaluado: los usuarios normales
solo administran sus bibliotecas. La importación real desde TMDB y RAWG queda
para después. Los datos de prueba son ficticios y no afirman existir en las API.
