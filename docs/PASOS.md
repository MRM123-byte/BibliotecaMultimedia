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

## 3. DTO, controladores y autenticación

Los DTO de entrada tienen solo campos editables. Los de salida omiten contraseñas,
tokens y propiedades internas de EF. Las validaciones de campos se ejecutan con
ApiController; las referencias inexistentes y reglas del dominio se comprueban
antes de guardar. SQL Server conserva la última protección ante duplicados o carreras.

Un POST correcto devuelve 201 y Location mediante CreatedAtAction. PUT reemplaza
los datos editables y devuelve 204; DELETE devuelve 204. Un recurso inexistente
devuelve 404, mientras que una referencia inválida en un cuerpo devuelve 400.

La biblioteca obtiene el usuario desde el token autenticado, nunca del cuerpo de
la petición. Una entrada ajena se trata como no encontrada para no revelar su
existencia. El catálogo se puede leer sin sesión, pero solo Catalogador puede
modificarlo. Los controladores privados admiten Bearer, no cookies.

Identity proporciona registro y login. Sus accessToken son tokens opacos, no JWT.
Swagger permite pegarlos en Authorize para probar los endpoints protegidos.
Los métodos usan operaciones asíncronas de EF y reciben DbContext por inyección.

## 4. Pruebas reproducibles

El script scripts/probar_api.py ejecuta peticiones HTTP reales; no usa una base
en memoria. Levanta un proceso en el puerto 5199, crea una ficha mediante POST,
la actualiza y reinicia ese proceso. El GET posterior recupera el mismo ID y
contenido desde SQL Server. El informe registra ambos PID y las respuestas.

Se verificaron además 400 por campos inválidos, referencias inexistentes y notas
fuera de rango; 404 por recursos inexistentes; y 401/403 para acceso no permitido.
La prueba de acceso cruzado confirma que un usuario no puede leer ni editar la
biblioteca de otro. Las fichas de evidencia se conservan para poder consultarlas.

Se acotó la paginación para evitar desbordamiento al calcular Skip y se separaron
las consultas de las colecciones relacionadas para evitar multiplicar filas.
