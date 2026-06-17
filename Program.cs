using OfficeOpenXml;

var builder = WebApplication.CreateBuilder(args);
ExcelPackage.License.SetNonCommercialOrganization("Indofood CBP");

// REGISTER REPOSITORY KE DI CONTAINER
builder.Services.AddScoped<DashboardTeknikP1.Repositories.HomeRepository>();
builder.Services.AddScoped<DashboardTeknikP1.Repositories.UploadRepository>();
builder.Services.AddScoped<DashboardTeknikP1.Repositories.TemuanRepository>();

// PERBAIKAN: Mendaftarkan SparepartRepository agar siap disuntikkan secara otomatis
builder.Services.AddScoped<DashboardTeknikP1.Repositories.SparepartRepository>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();