using Application.Services;
using Domain.DTOs.Requests;
using Domain.DTOs.Responses;
using Domain.Interfaces;
using Domain.Models;
using HealthChecks.UI.Client;
using HealthChecks.UI.Configuration;
using Infrastructure.BackgroundServices;
using Infrastructure.Data;
using Infrastructure.Middleware;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ML;
using Microsoft.IdentityModel.Tokens;
using Microsoft.ML;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Serilog;
using Serilog.Events;
using SwiftLine.API;
using SwiftLine.API.Extensions;
using SwiftLine.API.Extensions.Health;
using System.Net.Mail;
using System.Text;
using System.Threading.RateLimiting;


try
{
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
        .AddEnvironmentVariables()
        .Build();

    //var mlContext = new MLContext();
    //using var stream = File.OpenRead("Models/waitTimeModel.zip");
    //ITransformer trainedModel = mlContext.Model.Load(stream, out var schema);


    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .CreateLogger();

    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog to integrate with App Insights
    builder.Host.UseSerilog((context, services, loggerConfiguration) => 
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

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
    builder.Services.AddScoped<ISignalRNotifier, Notifier>();
    builder.Services.AddScoped<ISignalRNotifierRepo, SignalRNotifierRepo>();
    builder.Services.AddScoped<IFeedbackRepo, FeebackRepo>();
    builder.Services.AddScoped<IFeedbackService, FeedbackService>();
    builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
    builder.Services.AddScoped<IPushNotificationRepo, PushNotificationRepo>();
    builder.Services.AddScoped<IEmailsDeliveryRepo, EmailsDeliveryRepo>();

    builder.Services.Configure<IdentityOptions>(options =>
    {
        options.User.RequireUniqueEmail = true;

    });
    builder.Services.AddHostedService<LineManager>();
    builder.Services.AddHostedService<AccountsCleanup>();
    builder.Services.AddHostedService<EmailDeliveryJob>();
    //builder.Services.AddSingleton(mlContext);
    //builder.Services.AddSingleton(trainedModel);

    //builder.Services.AddPredictionEnginePool<QueueEntry, WaitTimePrediction>()
    //    .FromFile("waitTimeModel", "Models/waitTimeModel.zip");


    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
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

           options.Events = new JwtBearerEvents
           {
               OnMessageReceived = context =>
               {
                   // Check for token in query string for WebSocket connections
                   var accessToken = context.Request.Query["access_token"];

                   // If the request is for your hub
                   var path = context.HttpContext.Request.Path;
                   if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/queueHub"))
                   {
                       context.Token = accessToken;
                   }

                   return Task.CompletedTask;
               },
               OnChallenge = context =>
               {
                   context.Response.StatusCode = 401; // Unauthorized
                   context.Response.ContentType = "application/json";
                   return Task.CompletedTask;
               },
               OnAuthenticationFailed = context =>
               {
                   // Log the error or handle it as needed
                   Log.Error("Signal R Authentication failed: {Error}", context.Exception.Message);
                   return Task.CompletedTask;
               },

               

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
            policy.WithOrigins("http://localhost:5173", "https://swiftline-olive.vercel.app", "http://localhost:4173") // Replace with your client origin
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
            UseDefaultCredentials = false,
            EnableSsl = true,
            Credentials = new System.Net.NetworkCredential(
                builder.Configuration["Smtp:Username"],
            builder.Configuration["Smtp:Password"])
        }).AddRazorRenderer();

    //builder.Services.ConfigureHealthChecks(builder.Configuration);

    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    });

    builder.Services.AddSignalR();

    // Configure rate limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("GenericRestriction", opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1);    // Time window of 1 minute
            opt.PermitLimit = 30;                   
            opt.QueueLimit = 0;                      // Queue limit of 2
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });

        options.AddFixedWindowLimiter("LoginPolicy", opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1);
            opt.PermitLimit = 10;
            opt.QueueLimit = 0;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });
        options.AddFixedWindowLimiter("SignupPolicy", opt =>
        {
            opt.Window = TimeSpan.FromMinutes(1);
            opt.PermitLimit = 5;
            opt.QueueLimit = 0;
            opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        });
    });

    builder.Services.AddApplicationInsightsTelemetry();

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
    //HealthCheck Middleware
    //app.MapHealthChecks("/api/health", new HealthCheckOptions()
    //{
    //    Predicate = _ => true,
    //    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    //});

    //app.UseHealthChecksUI(delegate (Options options)
    //{
    //    options.UIPath = "/healthcheck-ui";
    //});

    app.UseRateLimiter();

    app.MapHub<SwiftLineHub>("/queueHub");

    app.MapControllers();

    await DbSeeder.SeedData(app);  // Call this method to seed the data

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw ex;
}
finally
{
    Log.CloseAndFlush();
}

public class LazyFactory<T> : Lazy<T> where T : class
{
    public LazyFactory(IServiceProvider provider)
        : base(() => provider.GetRequiredService<T>())
    {
    }
}

