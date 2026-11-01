using Scalar.AspNetCore;
using FreshMilk.API;
using FreshMilk.API.Middleware;
using FreshMilk.API.Services;
using FreshMilk.API.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// 1. CONTROLLERS AND OPENAPI
// =====================================================

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddFreshMilkDependencies();
builder.Services.AddHttpClient<IMsg91SmsService, Msg91SmsService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
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

app.UseMiddleware<ApiExceptionMiddleware>();

app.UseAuthorization();

// =====================================================
// 13. MAP CONTROLLERS
// =====================================================

app.MapControllers();

// =====================================================
// 14. RUN APPLICATION
// =====================================================

app.Run();