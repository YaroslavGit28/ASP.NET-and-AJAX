using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Todo Management API",
        Version = "v1",
        Description = "API для управления задачами. Поддерживает фильтрацию и создание.",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@example.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo Management API v1");
    });
}

var todos = new List<TodoItem>
{
    new(1, "Купить молоко", false),
    new(2, "Сделать зарядку", true)
};

app.MapGet("/api/todos", (bool? completed) =>
{
    var result = completed is null
        ? todos
        : todos.Where(t => t.IsCompleted == completed).ToList();

    return Results.Ok(result);
})
.WithName("GetTodos");

app.MapGet("/api/todos/{id:int}", (int id) =>
{
    var todo = todos.FirstOrDefault(t => t.Id == id);
    return todo is not null ? Results.Ok(todo) : Results.NotFound();
})
.WithName("GetTodoById");

app.MapPost("/api/todos", (TodoItem item) =>
{
    var newId = todos.Count == 0 ? 1 : todos.Max(t => t.Id) + 1;
    var newItem = item with { Id = newId };
    todos.Add(newItem);

    return Results.Created($"/api/todos/{newId}", newItem);
})
.WithName("CreateTodo");

app.Run();

record TodoItem(int Id, string Title, bool IsCompleted);