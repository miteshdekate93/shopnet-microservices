using IdentityService.Data;
using IdentityService.Models;
using IdentityService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<IdentityDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Token service
builder.Services.AddSingleton<TokenService>();

// JWT Authentication
var jwtSecret = builder.Configuration["JwtSettings:Secret"]
    ?? throw new InvalidOperationException("JWT secret is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = "identity-service",
            ValidateAudience = true,
            ValidAudience = "microservices",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Identity Service", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

var app = builder.Build();

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Identity Service v1"));
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

// ── Auth Endpoints ───────────────────────────────────────────────────────────
app.MapPost("/register", async (RegisterRequest request, IdentityDbContext db, TokenService tokenSvc) =>
{
    if (await db.Users.AnyAsync(u => u.Email == request.Email))
        return Results.Conflict(new { message = "Email already registered." });

    if (await db.Users.AnyAsync(u => u.Username == request.Username))
        return Results.Conflict(new { message = "Username already taken." });

    var user = new User
    {
        Email = request.Email,
        Username = request.Username,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        Role = "Customer",
        CreatedAt = DateTime.UtcNow
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    var token = tokenSvc.GenerateToken(user);
    return Results.Created($"/users/{user.Id}", new AuthResponse(user.Id, user.Username, user.Email, user.Role, token));
}).WithTags("Auth");

app.MapPost("/login", async (LoginRequest request, IdentityDbContext db, TokenService tokenSvc) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
    if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        return Results.Unauthorized();

    if (!user.IsActive)
        return Results.Forbid();

    user.LastLoginAt = DateTime.UtcNow;
    await db.SaveChangesAsync();

    var token = tokenSvc.GenerateToken(user);
    return Results.Ok(new AuthResponse(user.Id, user.Username, user.Email, user.Role, token));
}).WithTags("Auth");

app.MapPost("/validate-token", (TokenValidationRequest request, TokenService tokenSvc) =>
{
    var principal = tokenSvc.ValidateToken(request.Token);
    if (principal is null)
        return Results.Unauthorized();

    return Results.Ok(new
    {
        valid = true,
        userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
        username = principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value,
        role = principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value
    });
}).WithTags("Auth");

app.MapGet("/me", async (HttpContext ctx, IdentityDbContext db) =>
{
    var userIdClaim = ctx.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(userIdClaim, out var userId))
        return Results.Unauthorized();

    var user = await db.Users.FindAsync(userId);
    if (user is null) return Results.NotFound();

    return Results.Ok(new { user.Id, user.Username, user.Email, user.Role, user.CreatedAt, user.LastLoginAt });
}).RequireAuthorization().WithTags("Auth");

app.Run();

record RegisterRequest(string Email, string Username, string Password);
record LoginRequest(string Email, string Password);
record TokenValidationRequest(string Token);
record AuthResponse(int Id, string Username, string Email, string Role, string Token);
