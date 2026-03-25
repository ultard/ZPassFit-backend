using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using ZPassFit.Data;
using ZPassFit.Data.Dev;
using ZPassFit.Data.Models;
using ZPassFit.Data.Repositories.Attendance;
using ZPassFit.Data.Repositories.Clients;
using ZPassFit.Data.Repositories.Employees;
using ZPassFit.Data.Repositories.Memberships;
using ZPassFit.Services.Implementations;
using ZPassFit.Services.Interfaces;
using PredictionService = ZPassFit.Services.Implementations.PredictionService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IQrSessionRepository, QrSessionRepository>();
builder.Services.AddScoped<IVisitLogRepository, VisitLogRepository>();

builder.Services.AddScoped<IClientRepository, ClientRepository>();
builder.Services.AddScoped<ILevelRepository, LevelRepository>();
builder.Services.AddScoped<IClientLevelRepository, ClientLevelRepository>();
builder.Services.AddScoped<IBonusTransactionRepository, BonusTransactionRepository>();

builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();

builder.Services.AddScoped<IMembershipPlanRepository, MembershipPlanRepository>();
builder.Services.AddScoped<IMembershipRepository, MembershipRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();

builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IMembershipService, MembershipService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IPredictionService, PredictionService>();
builder.Services.AddGrpcClient<ZPassFit.Protos.PredictionService.PredictionServiceClient>(options =>
{
    var predictionServiceUrl = 
        builder.Configuration["Grpc:PredictionServiceUrl"]
        ?? throw new ArgumentNullException();

    options.Address = new Uri(predictionServiceUrl);
});

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.HttpOnly = true;
});

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    await DevelopmentSeed.EnsureSeededAsync(scope.ServiceProvider);
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();