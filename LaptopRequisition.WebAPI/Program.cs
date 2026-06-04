using LaptopRequisition.Application.Interfaces;
using LaptopRequisition.Application.Services;
using LaptopRequisition.Application.Configurations;
using LaptopRequisition.Infrastructure;
using LaptopRequisition.Infrastructure.Repositories;
using LaptopRequisition.Infrastructure.Services; 
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using LaptopRequisition.WebAPI.Middleware;
using MySql.EntityFrameworkCore;
using Refit;
using LaptopRequisition.Application.Interfaces.SSO;
using LaptopRequisition.Application.Interfaces.External;
using LaptopRequisition.Application.Extensions;
using LaptopRequisition.WebAPI.Controllers;
using LaptopRequisition.WebAPI.Services; 

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCustomSwagger(); 

builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("DefaultConnection"),
        mySqlOptions => mySqlOptions.EnableRetryOnFailure())); // Added EnableRetryOnFailure()


builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
builder.Services.AddScoped<ILaptopRepository, LaptopRepository>();
builder.Services.AddScoped<IRequestRepository, RequestRepository>();
builder.Services.AddScoped<IReturnRequestRepository, ReturnRequestRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>(); 
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>(); 
builder.Services.AddScoped<ILaptopAssignmentRepository, LaptopAssignmentRepository>(); // NEW: Register LaptopAssignmentRepository


builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
// REMOVED: builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<ILaptopService, LaptopService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReturnRequestService, ReturnRequestService>();
builder.Services.AddScoped<IOtpHelperService, OtpHelperService>();
builder.Services.AddScoped<IDashboardService, DashboardService>(); 
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>(); 
builder.Services.AddScoped<IRoleService, RoleService>(); 
builder.Services.AddScoped<IAdminReportingService, AdminReportingService>(); 
builder.Services.AddScoped<IRecycleBinService, RecycleBinService>(); 
builder.Services.AddHostedService<RecycleBinCleanupService>(); 


builder.Services.AddTransient<LoggingHandler>(); 


builder.Services.Configure<SsoSettings>(builder.Configuration.GetSection("SsoSettings"));
builder.Services.Configure<AdminSsoSettings>(builder.Configuration.GetSection("AdminSsoSettings")); // Configure AdminSsoSettings
builder.Services.Configure<OtpApiSettings>(builder.Configuration.GetSection("OtpApiSettings")); 
builder.Services.Configure<NotificationApiSettings>(builder.Configuration.GetSection("NotificationApiSettings")); 
builder.Services.Configure<AuthSettings>(builder.Configuration.GetSection("AuthSettings")); 
builder.Services.Configure<RecycleBinSettings>(builder.Configuration.GetSection("RecycleBinSettings"));



builder.Services.AddAuthPlatform(builder.Configuration); 
builder.Services.AddAuthorization(); 


builder.Services
    .AddRefitClient<ISsoClient>()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var ssoSettings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SsoSettings>>().Value;
        client.BaseAddress = new Uri(ssoSettings.BaseUrl);
    })
    .AddHttpMessageHandler<LoggingHandler>(); 

// Register IAdminSsoClient
builder.Services
    .AddRefitClient<IAdminSsoClient>()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var adminSsoSettings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<AdminSsoSettings>>().Value;
        client.BaseAddress = new Uri(adminSsoSettings.BaseUrl);
    })
    .AddHttpMessageHandler<LoggingHandler>(); 

builder.Services
    .AddRefitClient<IOtpApi>()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var otpApiSettings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OtpApiSettings>>().Value;
        client.BaseAddress = new Uri(otpApiSettings.BaseUrl);
    })
    .AddHttpMessageHandler<LoggingHandler>(); 

builder.Services
    .AddRefitClient<INotificationApi>()
    .ConfigureHttpClient((serviceProvider, client) =>
    {
        var notificationApiSettings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<NotificationApiSettings>>().Value;
        client.BaseAddress = new Uri(notificationApiSettings.BaseUrl);
    })
    .AddHttpMessageHandler<LoggingHandler>(); 


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        builder =>
        {
            builder.WithOrigins("https://laptop-requisition-form.vercel.app")
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
});


var app = builder.Build();


app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlingMiddleware>();


app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(x => x
    .SetIsOriginAllowed(origin => true)
    .AllowAnyMethod()
    .AllowAnyHeader()
    .AllowCredentials());

app.MapProfileEndpoint();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();