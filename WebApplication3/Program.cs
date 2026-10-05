using Microsoft.EntityFrameworkCore;
using WebApplication3.Application.Common.Abstractions;
using WebApplication3.Application.Orders.CancleOrder;
using WebApplication3.Application.Orders.CreateOrder;
using WebApplication3.Application.Orders.GetOrder;
using WebApplication3.Infrastructure;
using WebApplication3.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=webapplication3.db"));

builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddScoped<CreateOrderUseCase>();
builder.Services.AddScoped<GetOrderUseCase>();
builder.Services.AddScoped<CancelOrderUseCase>();

builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (!db.Customers.Any())
    {
        db.Customers.Add(new WebApplication3.Domain.Customer("ivan@example.com", "Иван Иванов"));
    }

    if (!db.Products.Any())
    {
        db.Products.AddRange(
            new WebApplication3.Domain.Product("Ноутбук", 75000m, 10),
            new WebApplication3.Domain.Product("Телефон", 50000m, 20),
            new WebApplication3.Domain.Product("Наушники", 5000m, 30));
    }

    db.SaveChanges();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.MapGet("/api/products", async (AppDbContext db, CancellationToken ct) =>
    await db.Products.AsNoTracking()
        .Select(p => new { p.Id, p.Name, p.Price, p.Stock })
        .ToListAsync(ct));

app.MapGet("/api/customers", async (AppDbContext db, CancellationToken ct) =>
    await db.Customers.AsNoTracking()
        .Select(c => new { c.Id, c.Email, c.Name, c.IsBlocked })
        .ToListAsync(ct));

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();

app.Run();
