var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Учебное хранилище в памяти. После перезапуска приложения данные сбрасываются.
var document = new Document("Исходный текст", 1);

static string GetETag(Document doc) => $"\"v{doc.Version}\"";

app.MapGet("/", () => Results.Ok(new
{
    message = "ETag demo is running",
    endpoints = new[]
    {
        "GET /doc - получить документ и ETag",
        "GET /doc with If-None-Match - проверить кэш (304)",
        "PUT /doc with If-Match - изменить документ с проверкой версии"
    }
}));

app.MapGet("/doc", (HttpContext context) =>
{
    var etag = GetETag(document);
    context.Response.Headers.ETag = etag;
    context.Response.Headers.CacheControl = "no-cache";

    var ifNoneMatch = context.Request.Headers.IfNoneMatch.ToString();
    if (ifNoneMatch == etag || ifNoneMatch == "*")
        return Results.StatusCode(StatusCodes.Status304NotModified);

    return Results.Ok(document);
});

app.MapPut("/doc", (HttpContext context, UpdateDocumentRequest incoming) =>
{
    var currentETag = GetETag(document);
    var clientETag = context.Request.Headers.IfMatch.ToString();

    if (string.IsNullOrWhiteSpace(clientETag))
    {
        return Results.Json(new
        {
            error = "Требуется заголовок If-Match",
            currentETag
        }, statusCode: StatusCodes.Status428PreconditionRequired);
    }

    if (clientETag != currentETag && clientETag != "*")
    {
        return Results.Json(new
        {
            error = "Документ был изменён другим клиентом",
            currentETag,
            yourETag = clientETag
        }, statusCode: StatusCodes.Status412PreconditionFailed);
    }

    document = document with
    {
        Text = incoming.Text,
        Version = document.Version + 1
    };

    var newETag = GetETag(document);
    context.Response.Headers.ETag = newETag;
    return Results.Ok(document);
});

app.Run();

public sealed record Document(string Text, int Version);
public sealed record UpdateDocumentRequest(string Text);
