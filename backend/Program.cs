var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddHttpClient<Backend.Services.TaalmodelService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});
builder.Services.AddSingleton<Backend.Services.KennisbankService>();
builder.Services.AddSingleton<Backend.Services.ZoekService>();
builder.Services.AddSingleton<Backend.Services.BestelStatusService>();
builder.Services.AddSingleton<Backend.Services.ToolRouterService>();
builder.Services.AddSingleton<Backend.Services.VertrouwenService>();
builder.Services.AddSingleton<Backend.Services.AuditService>();
builder.Services.AddSingleton<Backend.Services.SessieGeheugenService>();

var app = builder.Build();

app.UseCors("Frontend");

app.UseAuthorization();

app.MapControllers();

app.Run();

