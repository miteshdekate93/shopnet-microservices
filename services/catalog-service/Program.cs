using CatalogService.Data;
using CatalogService.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Catalog Service", Version = "v1" });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog Service v1"));

app.MapHealthChecks("/health");

// ── Categories ──────────────────────────────────────────────────────────────
app.MapGet("/categories", async (CatalogDbContext db) =>
    await db.Categories.ToListAsync())
    .WithTags("Categories");

app.MapGet("/categories/{id:int}", async (int id, CatalogDbContext db) =>
    await db.Categories.FindAsync(id) is Category c ? Results.Ok(c) : Results.NotFound())
    .WithTags("Categories");

app.MapPost("/categories", async (Category category, CatalogDbContext db) =>
{
    db.Categories.Add(category);
    await db.SaveChangesAsync();
    return Results.Created($"/categories/{category.Id}", category);
}).WithTags("Categories");

// ── Products ─────────────────────────────────────────────────────────────────
app.MapGet("/products", async (CatalogDbContext db) =>
    await db.Products.Include(p => p.Category).ToListAsync())
    .WithTags("Products");

app.MapGet("/products/{id:int}", async (int id, CatalogDbContext db) =>
    await db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id)
        is Product p ? Results.Ok(p) : Results.NotFound())
    .WithTags("Products");

app.MapPost("/products", async (Product product, CatalogDbContext db) =>
{
    product.CreatedAt = DateTime.UtcNow;
    product.UpdatedAt = DateTime.UtcNow;
    db.Products.Add(product);
    await db.SaveChangesAsync();
    return Results.Created($"/products/{product.Id}", product);
}).WithTags("Products");

app.MapPut("/products/{id:int}", async (int id, Product updated, CatalogDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    product.Name = updated.Name;
    product.Description = updated.Description;
    product.Price = updated.Price;
    product.Stock = updated.Stock;
    product.CategoryId = updated.CategoryId;
    product.UpdatedAt = DateTime.UtcNow;

    await db.SaveChangesAsync();
    return Results.Ok(product);
}).WithTags("Products");

app.MapDelete("/products/{id:int}", async (int id, CatalogDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();
    db.Products.Remove(product);
    await db.SaveChangesAsync();
    return Results.NoContent();
}).WithTags("Products");

app.Run();
