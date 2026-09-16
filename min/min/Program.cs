using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using min.DTOs;
using min.DTOs.V1;
using min.DTOs.V2;
using ProductService.MinimalAPI.Services;
using ProductService.MinimalAPI.Services.Repositories;
using ProductService.MinimalAPI.Middlewares;
using ProductService.MinimalAPI.Filters;
using ProductService.MinimalAPI.Validators;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. НАСТРОЙКА ЛОГИРОВАНИЯ
// ============================================================
builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration)
        .Enrich.WithProperty("Application", "ProductService.MinimalAPI")
        .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
        .WriteTo.Console()
        .WriteTo.File("logs/product-api-.txt", rollingInterval: RollingInterval.Day)
        .WriteTo.File("logs/product-api-error-.txt",
            rollingInterval: RollingInterval.Day,
            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error);
});

// ============================================================
// 2. РЕГИСТРАЦИЯ СЕРВИСОВ (DI)
// ============================================================
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null);
        sqlOptions.CommandTimeout(30);
    });
    options.LogTo(Console.WriteLine, LogLevel.Information);
});

// Регистрация репозиториев
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Регистрация сервисов
builder.Services.AddScoped<IProductService, ProductService>();

// Регистрация AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Регистрация валидаторов
builder.Services.AddValidatorsFromAssemblyContaining<ProductRequestValidator>();

// ============================================================
// 3. НАСТРОЙКА SWAGGER
// ============================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Product API v1",
        Version = "v1",
        Description = "Базовая версия API с форматированной ценой",
    });

    options.SwaggerDoc("v2", new()
    {
        Title = "Product API v2",
        Version = "v2",
        Description = "Расширенная версия API с дополнительными полями",
    });

    // JWT авторизация (пример)
    options.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Введите: Bearer {токен}"
    });

    options.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================================
// 4. НАСТРОЙКА CORS
// ============================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ============================================================
// 5. ПОСТРОЕНИЕ ПРИЛОЖЕНИЯ
// ============================================================
var app = builder.Build();

// ============================================================
// 6. MIDDLEWARE
// ============================================================
app.UseSerilogRequestLogging();

app.UseExceptionHandlingMiddleware();

app.UseCors("AllowAll");

// ============================================================
// 7. SWAGGER UI
// ============================================================
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
    options.SwaggerEndpoint("/swagger/v2/swagger.json", "API v2");
    options.RoutePrefix = "swagger";
});

// ============================================================
// 8. HEALTH CHECK
// ============================================================
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}))
.WithName("HealthCheck")
.WithOpenApi()
.Produces(200);

// ============================================================
// 9. ОСНОВНЫЕ ENDPOINTS (V1)
// ============================================================
var v1 = app.MapGroup("/api/v1/products")
    .WithTags("Products V1")
    .WithOpenApi();

// GET /api/v1/products - Получить все продукты
v1.MapGet("/", async (IProductService service) =>
{
    var result = await service.GetAllV1Async();
    return Results.Ok(result);
})
.WithName("GetAllProductsV1")
.WithDescription("Получение списка всех продуктов (базовая версия)")
.Produces<IEnumerable<ProductResponseV1>>(200);

// GET /api/v1/products/{id} - Получить продукт по ID
v1.MapGet("/{id:guid}", async (Guid id, IProductService service) =>
{
    var result = await service.GetByIdV1Async(id);
    return result == null
        ? Results.NotFound(new { error = $"Продукт с ID {id} не найден" })
        : Results.Ok(result);
})
.WithName("GetProductByIdV1")
.WithDescription("Получение продукта по ID (базовая версия)")
.Produces<ProductResponseV1>(200)
.Produces(404);

// POST /api/v1/products - Создать продукт
v1.MapPost("/", async (ProductRequestV1 request, IProductService service) =>
{
    var result = await service.CreateV1Async(request);
    return Results.Created($"/api/v1/products/{result.Id}", result);
})
.WithName("CreateProductV1")
.WithDescription("Создание нового продукта (базовая версия)")
.AddEndpointFilter<ValidationFilter<ProductRequestV1>>()
.Produces<ProductResponseV1>(201)
.ProducesValidationProblem(400);

// PUT /api/v1/products/{id} - Обновить продукт
v1.MapPut("/{id:guid}", async (Guid id, ProductRequestV1 request, IProductService service) =>
{
    var result = await service.UpdateV1Async(id, request);
    return result == null
        ? Results.NotFound(new { error = $"Продукт с ID {id} не найден" })
        : Results.Ok(result);
})
.WithName("UpdateProductV1")
.WithDescription("Обновление продукта (базовая версия)")
.AddEndpointFilter<ValidationFilter<ProductRequestV1>>()
.Produces<ProductResponseV1>(200)
.Produces(404)
.ProducesValidationProblem(400);

// DELETE /api/v1/products/{id} - Удалить продукт (soft delete)
v1.MapDelete("/{id:guid}", async (Guid id, IProductService service) =>
{
    var result = await service.DeleteAsync(id);
    return result
        ? Results.NoContent()
        : Results.NotFound(new { error = $"Продукт с ID {id} не найден" });
})
.WithName("DeleteProductV1")
.WithDescription("Удаление продукта (мягкое удаление)")
.Produces(204)
.Produces(404);

// ============================================================
// 10. РАСШИРЕННЫЕ ENDPOINTS (V2)
// ============================================================
var v2 = app.MapGroup("/api/v2/products")
    .WithTags("Products V2")
    .WithOpenApi();

// GET /api/v2/products - Получить все продукты (с доп. полями)
v2.MapGet("/", async (IProductService service) =>
{
    var result = await service.GetAllV2Async();
    return Results.Ok(result);
})
.WithName("GetAllProductsV2")
.WithDescription("Получение списка всех продуктов (расширенная версия)")
.Produces<IEnumerable<ProductResponseV2>>(200);

// GET /api/v2/products/{id} - Получить продукт по ID (расширенный)
v2.MapGet("/{id:guid}", async (Guid id, IProductService service) =>
{
    var result = await service.GetByIdV2Async(id);
    return result == null
        ? Results.NotFound(new { error = $"Продукт с ID {id} не найден" })
        : Results.Ok(result);
})
.WithName("GetProductByIdV2")
.WithDescription("Получение продукта по ID (расширенная версия)")
.Produces<ProductResponseV2>(200)
.Produces(404);

// POST /api/v2/products - Создать продукт (расширенный)
v2.MapPost("/", async (ProductRequestV2 request, IProductService service) =>
{
    var result = await service.CreateV2Async(request);
    return Results.Created($"/api/v2/products/{result.Id}", result);
})
.WithName("CreateProductV2")
.WithDescription("Создание нового продукта (расширенная версия)")
.AddEndpointFilter<ValidationFilter<ProductRequestV2>>()
.Produces<ProductResponseV2>(201)
.ProducesValidationProblem(400);

// ============================================================
// 11. ДОПОЛНИТЕЛЬНЫЙ ЭНДПОИНТ: ПОИСК ПО КАТЕГОРИИ
// ============================================================
app.MapGet("/api/products/search", async (string category, IProductService service) =>
{
    // Реализация через репозиторий напрямую
    var repository = app.Services.GetRequiredService<IProductRepository>();
    var entities = await repository.GetByCategoryAsync(category);
    var mapper = app.Services.GetRequiredService<AutoMapper.IMapper>();
    var result = mapper.Map<IEnumerable<ProductResponseV1>>(entities);

    return Results.Ok(result);
})
.WithName("SearchProductsByCategory")
.WithTags("Search")
.WithOpenApi()
.Produces<IEnumerable<ProductResponseV1>>(200)
.WithDescription("Поиск продуктов по категории (через V1 DTO)");

// ============================================================
// 12. ЗАПУСК
// ============================================================
app.Run();