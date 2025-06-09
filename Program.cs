using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using System.Text;
using System.Text.Json.Serialization;
using Supabase;

using Serilog;

var builder = WebApplication.CreateBuilder(args);

//Serilog configuration
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Replace default logger
builder.Host.UseSerilog();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key is not configured in appsettings.json or environment variables.");


builder.Services.AddHttpsRedirection(options =>
{
    options.HttpsPort = 7112;  // 
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5115);   // HTTP
    options.ListenLocalhost(7112, listenOptions =>   // HTTPS
    {
        listenOptions.UseHttps(); // enable HTTPS
    });
});


// clear existing authentication configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = context =>
        {
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            return Task.CompletedTask;
        }
    };
});



builder.Services.AddDbContext<AppDbContext>(options =>
options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddIdentityCore<User>()
    .AddEntityFrameworkStores<AppDbContext>();

// Supabase client

var supabaseUrl = builder.Configuration["Supabase:ProjectUrl"];
var supabaseKey = builder.Configuration["Supabase:ServiceRoleKey"];

var options = new SupabaseOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = true
};

var supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey, options);
await supabaseClient.InitializeAsync();
builder.Services.AddSingleton(async provider =>
{
    var options = new SupabaseOptions { AutoConnectRealtime = false };
    var client = new Client(
        "https://ashsitapkuigakmgrijr.supabase.co",
        builder.Configuration["Supabase:ServiceRoleKey"],
        options
    );

    await client.InitializeAsync();
    return client;
});

builder.Services.AddSingleton(supabaseClient);

builder.Services.AddTransient<SupabaseStorageService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
;

// Add CORS service
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:5173") 
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); 
    });
});



var app = builder.Build();

app.Use(async (context, next) =>
{
    await next.Invoke();
});

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
