using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;

var builder = WebApplication.CreateBuilder(args);

// Подключаем контроллеры
builder.Services.AddControllers();

var app = builder.Build();

var products = new List<ProductDto>
{
    new ProductDto(
        1,
        "Ноутбук",
        75000,
        new DateTime(2026, 9, 30, 10, 0, 0)
    ),

    new ProductDto(
        2,
        "Телефон",
        50000,
        new DateTime(2026, 9, 30, 10, 30, 0)
    ),

    new ProductDto(
        3,
        "Наушники",
        5000,
        new DateTime(2026, 9, 30, 11, 0, 0)
    )
};

// ==========================
// B2 — ETag
// ==========================

app.MapGet("/api/products/{id:int}",
    (int id, HttpRequest request, HttpResponse response) =>
{
    // Ищем товар
    var product = products.FirstOrDefault(p => p.Id == id);

    // Если товар не найден
    if (product is null)
    {
        return Results.NotFound();
    }

    // Формируем строку для создания ETag
    var source = $"{product.Id}-{product.UpdatedAt:O}";

    // Создаём SHA256-хэш
    var hash = SHA256.HashData(
        Encoding.UTF8.GetBytes(source)
    );

    // Получаем ETag
    var etag = $"\"{Convert.ToHexString(hash)}\"";

    // Передаём ETag клиенту
    response.Headers.ETag = etag;

    // Проверяем If-None-Match
    if (request.Headers.IfNoneMatch == etag)
    {
        return Results.StatusCode(
            StatusCodes.Status304NotModified
        );
    }

    // Возвращаем товар
    return Results.Ok(product);
});

// Подключаем Controllers
app.MapControllers();

app.Run();

public record ProductDto(
    int Id,
    string Name,
    decimal Price,
    DateTime UpdatedAt
);