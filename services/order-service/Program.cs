using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Order Service", Version = "v1" });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service v1"));

app.MapHealthChecks("/health");

// ── Orders ───────────────────────────────────────────────────────────────────
app.MapGet("/orders", async (OrderDbContext db) =>
    await db.Orders.Include(o => o.Items).ToListAsync())
    .WithTags("Orders");

app.MapGet("/orders/{id:int}", async (int id, OrderDbContext db) =>
    await db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id)
        is Order o ? Results.Ok(o) : Results.NotFound())
    .WithTags("Orders");

app.MapGet("/orders/customer/{customerId}", async (string customerId, OrderDbContext db) =>
    await db.Orders.Include(o => o.Items)
        .Where(o => o.CustomerId == customerId)
        .ToListAsync())
    .WithTags("Orders");

app.MapPost("/orders", async (Order order, OrderDbContext db) =>
{
    order.CreatedAt = DateTime.UtcNow;
    order.UpdatedAt = DateTime.UtcNow;
    order.Status = OrderStatus.Pending;
    order.Total = order.Items.Sum(i => i.Quantity * i.UnitPrice);

    db.Orders.Add(order);
    await db.SaveChangesAsync();
    return Results.Created($"/orders/{order.Id}", order);
}).WithTags("Orders");

app.MapPut("/orders/{id:int}/status", async (int id, OrderStatusUpdateRequest request, OrderDbContext db) =>
{
    var order = await db.Orders.FindAsync(id);
    if (order is null) return Results.NotFound();

    order.Status = request.Status;
    order.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(order);
}).WithTags("Orders");

app.MapDelete("/orders/{id:int}", async (int id, OrderDbContext db) =>
{
    var order = await db.Orders.FindAsync(id);
    if (order is null) return Results.NotFound();

    if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Cancelled)
        return Results.BadRequest("Only pending or cancelled orders can be deleted.");

    db.Orders.Remove(order);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithTags("Orders");

app.Run();

record OrderStatusUpdateRequest(OrderStatus Status);
