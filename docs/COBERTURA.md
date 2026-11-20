# Cobertura del diagrama en Swagger

Se contabilizan las entidades del diagrama y las operaciones que permiten usarlas.
**6 de 6 entidades representadas: 100 % de cobertura de entidades.** El profesor
debe confirmar si el 70 % se calcula por entidades u otro criterio; no equivale
a completar el 100 % del producto final o de las integraciones externas.

| Entidad | Operaciones disponibles |
|---|---|
| Usuario | POST auth/register, POST auth/login, GET usuarios/me |
| Contenido | GET listado y por ID, POST, PUT, DELETE |
| EntradaBiblioteca | GET listado y por ID, POST, PUT, DELETE; solo propietario |
| Genero | GET listado y por ID, POST, PUT, DELETE |
| ContenidoGenero | Asociación/desasociación con generoIds en POST/PUT de contenido; GET contenidos/{id}/generos |
| ReferenciaExterna | Alta/edición en POST/PUT de contenido; GET contenidos/{id}/referencias; borrado al eliminar la ficha |

Las entidades intermedias no necesitan un controlador separado para utilizar
sus relaciones. SaveChanges guarda cada operación de ficha de forma atómica.
El MVP usa una referencia por ficha. El esquema permite varias, pero aún no
se implementa la fusión de proveedores ni se permite dejar una ficha sin referencia.

## Reglas verificadas

- Una entrada por usuario y obra; nota opcional de 1 a 10, sin exigir Completado.
- Una reseña privada editable; NULL permite eliminarla.
- Anime es una clasificación de película o serie, sin duplicación.
- Campos y referencias inválidas producen 400; recursos inexistentes, 404.
- Las entradas ajenas tampoco se pueden consultar o modificar.
- Retirar una entrada conserva la ficha; eliminar fichas utilizadas se rechaza.
- POST devuelve 201 y Location; PUT y DELETE correctos, 204.
- GET tras reiniciar confirma persistencia real en SQL Server.

## Límites

No incluye frontend MVC, llamadas reales a proveedores, correo, funciones sociales
ni Android. Catalogador es un rol técnico para el CRUD, no un panel administrativo.
El código y el historial están publicados en el repositorio público
[MRM123-byte/BibliotecaMultimedia](https://github.com/MRM123-byte/BibliotecaMultimedia).
