using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.EntityFrameworkCore;
using NationalParkServiceAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Sites allowed to call the API from the browser (e.g. the deployed React site). Set in appsettings or Azure app settings.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));
// Log in returns a bearer token; the React app sends it back in the Authorization header.
builder.Services.AddAuthentication(BearerTokenDefaults.AuthenticationScheme)
    .AddBearerToken(options => options.BearerTokenExpiration = TimeSpan.FromDays(7));
builder.Services.AddAuthorization();
builder.Services.AddDbContext<NationalParkServiceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("NationalParkService")));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
