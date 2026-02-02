using System.Text;
using Aims.Api.Infrastructure.Data;
using Aims.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Built-in OpenAPI + 문서 변환기 등록(= Bearer security scheme 주입)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

// DbContext
builder.Services.AddDbContext<AimsDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("Default");
    options.UseNpgsql(cs);
});

// JWT Token Service
builder.Services.AddScoped<JwtTokenService>();

// Password hashing (User 엔티티용)
builder.Services.AddScoped<PasswordHasher<Aims.Api.Domain.Entities.User>>();

// AuthN/AuthZ
builder.Services.AddAuthorization();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var issuer = builder.Configuration["Jwt:Issuer"] ?? "AIMS";
        var audience = builder.Configuration["Jwt:Audience"] ?? "AIMS";
        var key = builder.Configuration["Jwt:Key"]
                  ?? throw new InvalidOperationException("Missing Jwt:Key");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidAudience = audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };

        // 401/403 응답 바디 통일
        options.Events = new JwtBearerEvents
        {
            OnChallenge = async ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                ctx.Response.ContentType = "application/json";

                await ctx.Response.WriteAsJsonAsync(new
                {
                    code = "AUTH_401",
                    message = "Unauthorized",
                    traceId = ctx.HttpContext.TraceIdentifier
                });
            },
            OnForbidden = async ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                ctx.Response.ContentType = "application/json";

                await ctx.Response.WriteAsJsonAsync(new
                {
                    code = "AUTH_403",
                    message = "Forbidden",
                    traceId = ctx.HttpContext.TraceIdentifier
                });
            }
        };
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // OpenAPI 문서(JSON)
    app.MapOpenApi();

    // Scalar UI
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();

internal sealed class BearerSecuritySchemeTransformer(
    IAuthenticationSchemeProvider authenticationSchemeProvider
) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken
    )
    {
        var schemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (!schemes.Any(s => s.Name == JwtBearerDefaults.AuthenticationScheme))
            return;

        // 문서 레벨 Security Scheme 등록
        var securitySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                In = ParameterLocation.Header,
                BearerFormat = "Json Web Token"
            }
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = securitySchemes;

        // 모든 operation에 Security requirement 적용(= Scalar에서 토큰 입력 UI 나오게)
        foreach (var op in document.Paths.Values.SelectMany(p => p.Operations))
        {
            op.Value.Security ??= [];
            op.Value.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        }
    }
}
