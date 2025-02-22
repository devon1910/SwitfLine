using Application.Services;
using Domain.Interfaces;
using Domain.Models;
using Infrastructure.BackgroundServices;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddIdentity<SwiftLineUser, IdentityRole>().AddEntityFrameworkStores<SwiftLineDatabaseContext>().AddDefaultTokenProviders();

builder.Services.AddDbContext<SwiftLineDatabaseContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("Database"));
});
builder.Services.AddOpenApi();

builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IEventRepo, EventRepo>();
builder.Services.AddScoped<ILineRepo, LineRepo>();
builder.Services.AddScoped<ILineService, LineService>();
builder.Services.AddScoped<ITokenRepo, TokenRepo>();
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
);

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
    // Use string values for enums
    // options.SchemaFilter<EnumSchemaFilter>();

});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
//app.MapIdentityApi<SwiftLineUser>();

app.UseHttpsRedirection();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "SwiftLine Demo");

});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await DbSeeder.SeedData(app);  // Call this method to seed the data

app.Run();
