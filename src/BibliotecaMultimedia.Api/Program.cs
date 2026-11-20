using Microsoft.OpenApi;
using BibliotecaMultimedia.Api.Data;
using BibliotecaMultimedia.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;
using BibliotecaMultimedia.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<BibliotecaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Biblioteca")));
builder.Services.AddIdentityApiEndpoints<Usuario>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Lockout.MaxFailedAccessAttempts = 5;
}).AddRoles<IdentityRole>().AddEntityFrameworkStores<BibliotecaDbContext>();
builder.Services.AddAuthorization(options => options.AddPolicy("UsuarioApi", policy =>
    policy.AddAuthenticationSchemes(IdentityConstants.BearerScheme).RequireAuthenticatedUser()));
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues:false)));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Biblioteca Multimedia API",
        Version = "v1",
        Description = "Mauricio Rivero Mendez. Inicia sesión en /api/auth/login (useCookies=false), copia accessToken y pégalo en Authorize. Catálogo: lectura pública y escritura para Catalogador. Biblioteca: acceso privado. Datos de demostración ficticios; sin llamadas externas."
    });
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory,"BibliotecaMultimedia.Api.xml"));
    options.AddSecurityDefinition("Bearer",new OpenApiSecurityScheme
    {
        Type=SecuritySchemeType.Http,Scheme="bearer",
        Description="Token opaco de ASP.NET Core Identity (no es un JWT). Pega solamente accessToken."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer",document)]=[]
    });
});

var app = builder.Build();
if(args.Contains("--seed"))
{
    if(!app.Environment.IsDevelopment()) throw new InvalidOperationException("La carga de prueba solo se permite en Development.");
    await DatosPrueba.CargarAsync(app.Services,app.Configuration);
    Console.WriteLine("Datos de prueba cargados. Las cuentas y obras existentes no fueron reemplazadas.");
    return;
}
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapGroup("/api/auth").WithTags("Autenticacion").MapIdentityApi<Usuario>();
app.MapControllers();
app.Run();
