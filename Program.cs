using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SistemaDoacaoSangue.Models;
using Microsoft.AspNetCore.Authorization;
using HotChocolate.Data.Filters;
using SistemaDoacaoSangue.Data;
using SistemaDoacaoSangue.Services;

var builder = WebApplication.CreateBuilder(args);

// Configura��o do Entity Framework Core
builder.Services.AddDbContext<SistemaDoacaoSangueContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configura��o de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", builder =>
    {
        builder.AllowAnyHeader()
               .AllowAnyMethod()
               .AllowAnyOrigin();
    });
});


// Configura��o de autentica��o JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddAuthorization();

builder.Services
    .AddGraphQLServer()
    .InitializeOnStartup()
    .AddQueryType<Query>()
    .AddProjections()
    .AddFiltering()
    .AddSorting()
    .AddMutationType<Mutation>();

builder.Services.AddDbContextFactory<SistemaDoacaoSangueContext>(opt =>
    opt.UseSqlServer(StrConnection.Connection),
    ServiceLifetime.Scoped
);

builder.Services.AddControllers();

var app = builder.Build();

app.UseCors("DefaultPolicy");
app.UseAuthentication();
app.UseAuthorization();

// Configura��o do GraphQL
app.MapGraphQL();

app.MapControllers();

app.Run();
