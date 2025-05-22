using ProductAPIRedisCache.Application.Interfaces;
using ProductAPIRedisCache.Application.Services;
using ProductAPIRedisCache.Database;
using ProductAPIRedisCache.Domain.Interfaces;
using ProductAPIRedisCache.Infrastructure.Cache;
using ProductAPIRedisCache.Infrastructure.Repositories;
using ProductAPIRedisCache.Middleware;
using StackExchange.Redis;
using Serilog;
using CorrelationId.DependencyInjection;
 

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10)
    .WriteTo.Seq("http://localhost:5341") // Seq
     
    .CreateLogger();

builder.Services.AddControllers();

builder.Services.AddScoped<IDbConnectionFactory, SqlConnectionFactory>();
builder.Services.AddScoped<IProductRepository, ProductRepository>(); 
builder.Services.AddScoped<IProductService, ProductService>();

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost:6379"));
builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Host.UseSerilog();

builder.Services.AddDefaultCorrelationId(options =>
{
    options.AddToLoggingScope = true;
    options.RequestHeader = "X-Correlation-Id";
    options.ResponseHeader = "X-Correlation-Id";
    options.UpdateTraceIdentifier = true;
    options.IncludeInResponse = true;
});

var app = builder.Build(); 

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();


app.UseMiddleware<ExceptionMiddleware>(); 

app.MapControllers();
app.Run();
