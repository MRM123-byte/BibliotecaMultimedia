using Microsoft.OpenApi;
using BibliotecaMultimedia.Api.Data;
using BibliotecaMultimedia.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<BibliotecaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Biblioteca")));
builder.Services.AddIdentityApiEndpoints<Usuario>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
    options.Lockout.MaxFailedAccessAttempts = 5;
}).AddRoles<IdentityRole>().AddEntityFrameworkStores<BibliotecaDbContext>();
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => options.SwaggerDoc("v1", new OpenApiInfo
{
    Title = "Biblioteca Multimedia API",
    Version = "v1",
    Description = "Proyecto de Programación Web III - Mauricio Rivero Mendez"
}));

var app = builder.Build();
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
