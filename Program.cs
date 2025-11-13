using Microsoft.EntityFrameworkCore;
using CalculadoraEVTC.Data;
using CalculadoraEVTC.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog para logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/calculadora-evtc-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Adicionar serviços
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configurar DbContext com SQLite
builder.Services.AddDbContext<CalculadoraContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrar serviços
builder.Services.AddScoped<ICalculadoraService, CalculadoraService>();
builder.Services.AddScoped<ICotacaoRepository, CotacaoRepository>();

var app = builder.Build();

// Inicializar banco de dados
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<CalculadoraContext>();
    context.Database.EnsureCreated();
    Log.Information("Banco de dados SQLite inicializado");
}

// Configurar pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

Log.Information("Aplicação Calculadora EVTC iniciada");
app.Run();