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

// ---------- Start ----------
await app.RunAsync();
