using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MotasAlcoafinal.Models;
using MotasAlcoafinal.Services;
using MotasAlcoafinal.Data;

var builder = WebApplication.CreateBuilder(args);

// ---------- Servi�os ----------
builder.Services.AddDbContext<MotasAlcoaContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity (utilizador e roles)
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
})
.AddEntityFrameworkStores<MotasAlcoaContext>()
.AddDefaultTokenProviders();


builder.Services.AddRazorPages();


builder.Services.AddControllersWithViews();

// Servi�o de email
builder.Services.AddTransient<EmailService>();

// Configura��o de cookies 
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login"; // Redireciona para login se n�o autenticado
});

// Eliminar a prote��o de 'ciclos' qd se faz uma pesquisa que envolva um relacionamento 1-N em Linq
// https://code-maze.com/aspnetcore-handling-circular-references-when-working-with-json/
// https://marcionizzola.medium.com/como-resolver-jsonexception-a-possible-object-cycle-was-detected-27e830ea78e5
builder.Services.AddControllers()
                .AddJsonOptions(options => options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

// *******************************************************************
// Instalar o package
// Microsoft.AspNetCore.Authentication.JwtBearer
//
// using Microsoft.IdentityModel.Tokens;
// *******************************************************************
// JWT Settings
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(options => { })
   .AddCookie("Cookies", options => {
       options.LoginPath = "/Identity/Account/Login";
       options.AccessDeniedPath = "/Identity/Account/AccessDenied";
   })
   .AddJwtBearer("Bearer", options => {
       options.TokenValidationParameters = new TokenValidationParameters
       {
           ValidateIssuer = true,
           ValidateAudience = true,
           ValidateLifetime = true,
           ValidateIssuerSigningKey = true,
           ValidIssuer = jwtSettings["Issuer"],
           ValidAudience = jwtSettings["Audience"],
           IssuerSigningKey = new SymmetricSecurityKey(key),

           RoleClaimType = ClaimTypes.Role,
       };
   });


// configura��o do JWT
builder.Services.AddScoped<TokenService>();

// Adiciona o Swagger
// builder.Services.AddEndpointsApiExplorer();   // necess�ria apenas para APIs m�nimas. 
//builder.Services.AddSwaggerGen();

builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Minha API de gest�o de uma oficina de motos",
        Version = "v1",
        Description = "API para gest�o de clientes, encomendas, servi�os, pe�as e motocicletas"
    });

    // Caminho para o XML gerado
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    
    c.IncludeXmlComments(xmlPath);

});

//declarar o servi�o do Signal R 
builder.Services.AddSignalR();

var app = builder.Build();

// ---------- Middlewares ----------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Seguran�a HTTPS
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Autentica��o e Autoriza��o
app.UseAuthentication();
app.UseAuthorization();

// ---------- Endpoints ----------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// ---------- Seed de Roles/Admin ----------
using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

//criar uma ponte enrre o nosso servi�o signal R (o ServicosHub)
//e o javascript do browser
app.MapHub<ServicosHub>("/servicoshub");


// ---------- Start ----------
await app.RunAsync();
