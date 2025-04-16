using Application.Services;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.BackgroundServices;
using Infrastructure.Data;
using Infrastructure.Middleware;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SwiftLine.API;
using SwiftLine.API.Extensions;
using System.Net.Mail;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddIdentity<SwiftLineUser, IdentityRole>().AddEntityFrameworkStores<SwiftLineDatabaseContext>().AddDefaultTokenProviders();

builder.Services.AddDbContext<SwiftLineDatabaseContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database"));
});
builder.Services.AddOpenApi();

builder.Services.AddTransient(typeof(Lazy<>), typeof(LazyFactory<>));
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IEventRepo, EventRepo>();
builder.Services.AddScoped<ILineRepo, LineRepo>();
builder.Services.AddScoped<ILineService, LineService>();
builder.Services.AddScoped<ITokenRepo, TokenRepo>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuthRepo, AuthRepo>();
builder.Services.AddScoped<INotifier, Notifier>();
builder.Services.AddScoped<INotifierRepo, NotifierRepo>();
builder.Services.Configure<IdentityOptions>(options =>
{
    options.User.RequireUniqueEmail = true;

});
builder.Services.AddHostedService<LineManager>();


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}
)
   .AddJwtBearer(options =>
   {
       options.SaveToken = true;
       options.RequireHttpsMetadata = false;
       options.TokenValidationParameters = new TokenValidationParameters
       {
           ValidateIssuer = true,
           ValidateAudience = true,
           ValidAudience = builder.Configuration["JWT:ValidAudience"],
           ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
           ClockSkew = TimeSpan.Zero,
           IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:secret"]))
       };
   }
).AddCookie().AddGoogle(options =>
{
    var clientId= builder.Configuration["Authentication:Google:ClientId"];
    var clientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

    if (clientId is null) throw new ArgumentNullException("ClientId is required");
    if (clientSecret is null) throw new ArgumentNullException("ClientSecret is required");

    options.ClientId = clientId;
    options.ClientSecret = clientSecret;
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
});

builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = "API", Version = "v1" });
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Place to add JWT with Bearer token",
            Name = "Authorization",
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Name = "Bearer"
                        }, new List<string>()
                    }
        });

    });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://swiftline-olive.vercel.app") // Replace with your client origin
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR with credentials

    });
});
builder.Services.AddFluentEmail(builder.Configuration["Smtp:FromEmail"]) 
    .AddSmtpSender(new SmtpClient
    {
        Host = builder.Configuration["Smtp:Host"],
        Port = int.Parse(builder.Configuration["Smtp:Port"]),
        EnableSsl = true,
        Credentials = new System.Net.NetworkCredential(
            builder.Configuration["Smtp:Username"],
        builder.Configuration["Smtp:Password"])
    }).AddRazorRenderer();



builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services.AddSignalR(options =>
{
    //options.EnableDetailedErrors = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapOpenApi();
if (app.Environment.IsDevelopment())
{
    app.ApplyMigrations();
}
//app.MapIdentityApi<SwiftLineUser>();

app.UseHttpsRedirection();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "SwiftLine");

});
app.UseCors();
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();


app.MapHub<SwiftLineHub>("/queueHub");

app.MapControllers();

await DbSeeder.SeedData(app);  // Call this method to seed the data

app.Run();

public class LazyFactory<T> : Lazy<T> where T : class
{
    public LazyFactory(IServiceProvider provider)
        : base(() => provider.GetRequiredService<T>())
    {
    }
}

