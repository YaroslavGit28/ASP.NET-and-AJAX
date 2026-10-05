using System.Diagnostics;
using ASP_pystoy.Middleware;
using ASP_pystoy.Models;
using ASP_pystoy.Services;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. СТЯ ССТ (DI LIFETIMES)
// ==========================================

// онитор состояния хоста и истории запросов (Singleton)
builder.Services.AddSingleton<IAppLifetimeMonitor, AppLifetimeMonitor>();

// емонстрация 3-х жизненных циклов DI:
// 1. Transient: создается каждый раз заново
builder.Services.AddTransient<IOperationTransient, Operation>();

// 2. Scoped: создается один раз на каждый HTTP-запрос
builder.Services.AddScoped<IOperationScoped, Operation>();

// 3. Singleton: создается один раз на всё время жизни хоста
builder.Services.AddSingleton<IOperationSingleton, Operation>();

// Сервис, использующий все три операции для демонстрации их поведения
builder.Services.AddTransient<IOperationService, OperationService>();

var app = builder.Build();

// ==========================================
// 2. Ы  ХСТ (IHostApplicationLifetime)
// ==========================================
var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
var monitor = app.Services.GetRequiredService<IAppLifetimeMonitor>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

lifetime.ApplicationStarted.Register(() =>
{
    monitor.SetHostStatus("Running");
    monitor.RecordEvent("ApplicationStarted", "риложение успешно запущено и готово к приему входящих запросов.");

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("""
╔═══════════════════════════════════════════════════════════════════════════════════╗
║                   [HOST LIFECYCLE: ApplicationStarted]                            ║
║  Хост приложения ASP.NET Core полностью сконфигурирован и успешно запущен!        ║
║  Службы DI, кастомные Middleware и обработчики маршрутов активны.                  ║
╚═══════════════════════════════════════════════════════════════════════════════════╝
""");
    Console.ResetColor();
});

lifetime.ApplicationStopping.Register(() =>
{
    monitor.SetHostStatus("Stopping");
    monitor.RecordEvent("ApplicationStopping", "нициирован процесс корректной остановки хоста (Graceful Shutdown).");

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("""
╔═══════════════════════════════════════════════════════════════════════════════════╗
║                   [HOST LIFECYCLE: ApplicationStopping]                           ║
║  олучен сигнал завершения работы приложения (SIGINT/Ctrl+C).                     ║
║  авершение активных запросов и освобождение ресурсов...                          ║
╚═══════════════════════════════════════════════════════════════════════════════════╝
""");
    Console.ResetColor();
});

lifetime.ApplicationStopped.Register(() =>
{
    monitor.SetHostStatus("Stopped");
    monitor.RecordEvent("ApplicationStopped", "се службы остановлены. риложение полностью завершило работу.");

    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("""
╔═══════════════════════════════════════════════════════════════════════════════════╗
║                   [HOST LIFECYCLE: ApplicationStopped]                            ║
║  Хост приложения ASP.NET Core завершил работу. есурсы освобождены.               ║
╚═══════════════════════════════════════════════════════════════════════════════════╝
""");
    Console.ResetColor();
});

// ==========================================
// 3.  Т С (MIDDLEWARE)
// ==========================================

// 1) астомный Middleware для логирования жизненного цикла каждого HTTP-запроса
app.UseRequestLifecycle();

// 2) астомный Middleware для перехвата и валидации данных формы
app.UseFormProcessing();

// 3) аздача статических файлов и главной страницы из wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// ==========================================
// 4. ШТЫ  Ы Т (ENDPOINTS)
// ==========================================

// API для получения данных жизненного цикла хоста, DI и журнала запросов
app.MapGet("/api/lifecycle", (
    IOperationTransient transientOp,
    IOperationScoped scopedOp,
    IOperationSingleton singletonOp,
    IOperationService opService,
    IAppLifetimeMonitor appMonitor) =>
{
    return Results.Ok(new
    {
        hostStatus = appMonitor.HostStatus,
        uptime = $"{appMonitor.Uptime.Hours:D2}ч {appMonitor.Uptime.Minutes:D2}м {appMonitor.Uptime.Seconds:D2}с",
        hostStartTime = appMonitor.HostStartTime,
        events = appMonitor.LifecycleEvents,
        di = new
        {
            transient1 = transientOp.OperationId.ToString(),
            transient2 = opService.TransientOperation.OperationId.ToString(),
            scoped1 = scopedOp.OperationId.ToString(),
            scoped2 = opService.ScopedOperation.OperationId.ToString(),
            singleton1 = singletonOp.OperationId.ToString(),
            singleton2 = opService.SingletonOperation.OperationId.ToString()
        },
        recentRequests = appMonitor.RecentRequests
    });
});

// Обработчик отправки формы (поддерживает обычную отправку и AJAX)
app.MapPost("/submit", (
    HttpContext context,
    IOperationTransient transientOp,
    IOperationScoped scopedOp,
    IOperationSingleton singletonOp,
    IOperationService opService,
    IAppLifetimeMonitor appMonitor) =>
{
    // Проверяем ошибки валидации, зафиксированные Middleware
    if (context.Items.TryGetValue("ValidationError", out var validationError) && validationError != null)
    {
        return Results.BadRequest(new
        {
            success = false,
            message = validationError.ToString()
        });
    }

    var name = context.Items["FormName"]?.ToString() ?? "Не указано";
    var email = context.Items["FormEmail"]?.ToString() ?? "Не указано";
    var category = context.Items["FormCategory"]?.ToString() ?? "Общие вопросы";
    var message = context.Items["FormMessage"]?.ToString() ?? "";
    var requestId = context.Items["RequestId"]?.ToString() ?? Guid.NewGuid().ToString("N")[..8];

    var formEntry = new FeedbackFormModel
    {
        Name = name,
        Email = email,
        Category = category,
        Message = message,
        RequestId = requestId,
        SubmittedAt = DateTime.UtcNow
    };

    appMonitor.RecordFormSubmission(formEntry);

    var isAjax = context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                 context.Request.Headers.Accept.ToString().Contains("application/json");

    var responseData = new
    {
        success = true,
        message = "Форма успешно принята и обработана сервером!",
        requestId,
        middlewareValidated = context.Items.ContainsKey("MiddlewareProcessed"),
        receivedData = new
        {
            name,
            email,
            category,
            messageLength = message.Length
        },
        transientOperationId = transientOp.OperationId.ToString(),
        scopedOperationId = scopedOp.OperationId.ToString(),
        singletonOperationId = singletonOp.OperationId.ToString()
    };

    if (isAjax)
    {
        return Results.Ok(responseData);
    }

    var html = $@"
<!DOCTYPE html>
<html lang=""ru"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Форма обработана</title>
    <style>
        :root {{
            --bg-1: #f4f7ff;
            --bg-2: #eaf3ff;
            --panel: rgba(255,255,255,0.78);
            --line: rgba(146, 160, 198, 0.28);
            --text: #1d2440;
            --muted: #58698d;
            --primary: #5b6cff;
            --primary-2: #7a4dff;
            --success: #1bb56d;
            --shadow: 0 24px 64px rgba(91, 108, 255, 0.18);
        }}

        * {{ box-sizing: border-box; }}

        body {{
            margin: 0;
            min-height: 100vh;
            display: grid;
            place-items: center;
            font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;
            background:
                radial-gradient(circle at top left, rgba(91,108,255,0.16), transparent 24%),
                radial-gradient(circle at bottom right, rgba(122,77,255,0.14), transparent 26%),
                linear-gradient(135deg, var(--bg-1), var(--bg-2));
            color: var(--text);
        }}

        .panel {{
            width: min(680px, calc(100vw - 32px));
            background: var(--panel);
            border: 1px solid var(--line);
            backdrop-filter: blur(18px);
            border-radius: 28px;
            box-shadow: var(--shadow);
            padding: 32px 28px;
        }}

        .icon {{
            width: 76px;
            height: 76px;
            border-radius: 24px;
            display: grid;
            place-items: center;
            margin: 0 auto 18px;
            font-size: 2rem;
            font-weight: 800;
            background: linear-gradient(135deg, var(--success), #53d19d);
            color: white;
            box-shadow: 0 18px 35px rgba(27, 181, 109, 0.24);
        }}

        .eyebrow {{
            margin: 0 0 8px;
            text-align: center;
            text-transform: uppercase;
            letter-spacing: 0.12em;
            font-size: 0.72rem;
            color: #4b67d7;
            font-weight: 800;
        }}

        h1 {{
            margin: 0;
            text-align: center;
            font-size: clamp(1.8rem, 3vw, 2.6rem);
            letter-spacing: -0.04em;
        }}

        .grid {{
            display: grid;
            grid-template-columns: repeat(2, minmax(0, 1fr));
            gap: 14px 18px;
            margin-top: 24px;
            margin-bottom: 28px;
        }}

        .item {{
            background: rgba(255,255,255,0.72);
            border: 1px solid var(--line);
            border-radius: 14px;
            padding: 12px 14px;
        }}

        .label {{
            display: block;
            font-size: 0.7rem;
            text-transform: uppercase;
            letter-spacing: 0.08em;
            font-weight: 800;
            color: var(--muted);
            margin-bottom: 6px;
        }}

        .value {{
            display: block;
            font-weight: 700;
            color: var(--text);
            word-break: break-word;
        }}

        .primary-btn {{
            display: inline-flex;
            align-items: center;
            justify-content: center;
            gap: 8px;
            width: 100%;
            padding: 14px 18px;
            border-radius: 14px;
            text-decoration: none;
            color: white;
            font-weight: 800;
            background: linear-gradient(135deg, var(--primary), var(--primary-2));
            box-shadow: 0 16px 28px rgba(91,108,255,0.25);
        }}

        @media (max-width: 560px) {{
            .grid {{
                grid-template-columns: 1fr;
            }}

            .panel {{
                padding: 24px 18px;
            }}
        }}
    </style>
</head>
<body>
    <div class=""panel"">
        <div class=""icon"">✓</div>
        <p class=""eyebrow"">Заявка принята</p>
        <h1>Форма успешно отправлена</h1>

        <div class=""grid"">
            <div class=""item"">
                <span class=""label"">Имя</span>
                <span class=""value"">{name}</span>
            </div>
            <div class=""item"">
                <span class=""label"">Email</span>
                <span class=""value"">{email}</span>
            </div>
            <div class=""item"">
                <span class=""label"">Категория</span>
                <span class=""value"">{category}</span>
            </div>
            <div class=""item"">
                <span class=""label"">Request ID</span>
                <span class=""value"">{requestId}</span>
            </div>
        </div>

        <a href=""/"" class=""primary-btn"">← Вернуться на главную</a>
    </div>
</body>
</html>
";

    return Results.Content(html, "text/html; charset=utf-8");
});

app.Run();
