using Microsoft.AspNetCore.Authentication.Cookies;
using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);
ExcelPackage.License.SetNonCommercialOrganization("Indofood CBP");

// REGISTER REPOSITORY
builder.Services.AddScoped<DashboardTeknikP1.Repositories.HomeRepository>();
builder.Services.AddScoped<DashboardTeknikP1.Repositories.UploadRepository>();
builder.Services.AddScoped<DashboardTeknikP1.Repositories.TemuanRepository>();
builder.Services.AddScoped<DashboardTeknikP1.Repositories.SparepartRepository>();
builder.Services.AddScoped<DashboardTeknikP1.Repositories.PemakaianRepository>();

// ==========================================================
// TAMBAHAN BARU: Mendaftarkan Sistem Keamanan (Cookie Auth)
// ==========================================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login"; // Jika belum login, lempar ke sini
        options.LogoutPath = "/Auth/Logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // Sesi login kedaluwarsa dalam 8 jam (1 Shift)
    });

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// ==========================================================
// TAMBAHAN BARU: Mengaktifkan Satpam Pengecek KTP Digital
// ==========================================================
app.UseAuthentication(); // Harus dipanggil SEBELUM UseAuthorization
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();