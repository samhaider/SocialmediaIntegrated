using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SocialMediaAPI.Configuration;
using SocialMediaAPI.Data;
using SocialMediaAPI.Interfaces;
using SocialMediaAPI.Services;
using SocialMediaAPI.Services.Instagram;
using SocialMediaAPI.Services.Platforms;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure Entity Framework with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Configure Instagram settings
builder.Services.Configure<InstagramSettings>(builder.Configuration.GetSection("Instagram"));

// Register HttpClient for Instagram API calls
builder.Services.AddHttpClient<IInstagramGraphApiService, InstagramGraphApiService>();

// Register Instagram Graph API service
builder.Services.AddScoped<IInstagramGraphApiService, InstagramGraphApiService>();

// Register application services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPostService, PostService>();
builder.Services.AddScoped<ISocialAccountService, SocialAccountService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IMediaService, MediaService>();

// Register background services
builder.Services.AddHostedService<PostSchedulerService>();

// Register social media platform implementations (13+ platforms)
builder.Services.AddScoped<ISocialMediaPlatform, FacebookPlatform>();
builder.Services.AddScoped<ISocialMediaPlatform, TwitterPlatform>();
builder.Services.AddScoped<ISocialMediaPlatform, InstagramPlatform>();
builder.Services.AddScoped<ISocialMediaPlatform, LinkedInPlatform>();
builder.Services.AddScoped<ISocialMediaPlatform, TikTokPlatform>();
builder.Services.AddScoped<ISocialMediaPlatform, YouTubePlatform>();
builder.Services.AddScoped<ISocialMediaPlatform, PinterestPlatform>();
builder.Services.AddScoped<ISocialMediaPlatform, SnapchatPlatform>();
builder.Services.AddScoped<ISocialMediaPlatform, RedditPlatform>();
builder.Services.AddScoped<ISocialMediaPlatform, TumblrPlatform>();
builder.Services.AddScoped<ISocialMediaPlatform, MediumPlatform>();
builder.Services.AddScoped<ISocialMediaPlatform, VKPlatform>();
builder.Services.AddScoped<ISocialMediaPlatform, TelegramPlatform>();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Social Media Management API",
        Version = "v1",
        Description = "A unified REST API that connects to 13+ social networks through a single interface"
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Social Media Management API v1");
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
