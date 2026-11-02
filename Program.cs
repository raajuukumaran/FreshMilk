using Scalar.AspNetCore;
using FreshMilk.API;
using FreshMilk.API.Middleware;
using FreshMilk.API.Services;
using FreshMilk.API.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.FileProviders;
using FreshMilk.API.Hubs;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// 1. CONTROLLERS AND OPENAPI
// =====================================================

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSignalR();
builder.Services.AddDataProtection()
    .SetApplicationName("FreshMilk.API");
builder.Services.AddFreshMilkDependencies();
builder.Services.AddHttpClient<IMsg91SmsService, Msg91SmsService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddHttpClient<IExpoPushService, ExpoPushService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(8);
});

// =====================================================
// 2. CORS CONFIGURATION
// =====================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("FreshMilkCors", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// =====================================================
// 3. BUILD APPLICATION
// =====================================================

var app = builder.Build();

var profileImageDirectory =
    builder.Configuration["ProfileImages:Directory"] ??
    Path.Combine(app.Environment.ContentRootPath, "wwwroot", "profile-images");
Directory.CreateDirectory(profileImageDirectory);

// =====================================================
// 10. OPENAPI AND SCALAR
// =====================================================

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// =====================================================
// 11. ROOT ENDPOINT
// =====================================================

app.MapGet("/", () => new
{
    success = true,
    message = "FreshMilk API is running!"
});

// =====================================================
// 12. MIDDLEWARE PIPELINE
// =====================================================

app.UseCors("FreshMilkCors");

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(profileImageDirectory),
    RequestPath = "/profile-images"
});

app.UseMiddleware<ApiExceptionMiddleware>();

app.UseAuthorization();

// =====================================================
// 13. MAP CONTROLLERS
// =====================================================

app.MapControllers();
app.MapHub<ChatCallHub>("/hubs/calls");

// =====================================================
// 14. RUN APPLICATION
// =====================================================

app.Run();