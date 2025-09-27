
using AuthAPI.Core.Application.Interfaces;
using AuthAPI.Core.Application.Options;
using AuthAPI.Core.Application.Services;
using AuthAPI.Core.Domain.Entities;
using AuthAPI.Infrastructure.Persistence;
using Google.Cloud.SecretManager.V1;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Dummy.SharedLib.Abstract;

var builder = WebApplication.CreateBuilder(args);

var tempCertPath = GetTempPath();
var certContent = GetCertContent();
await File.WriteAllTextAsync(tempCertPath, certContent);

// Configure EF Core with SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddScoped(typeof(IRepository<,>), typeof(PostgresRepository<,>));

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("ApiSettings:JwtOptions"));
// Register interfaces and implementations for Dependency Injection
builder.Services.AddIdentity<ApplicationUser, IdentityRole>().AddDefaultTokenProviders()
    .AddEntityFrameworkStores<AppDbContext>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 2. Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => "OK");
ApplyMigrations();
app.Run();

string GetTempPath()
{
    string tempCertPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.crt");
    return tempCertPath;
}

string GetCertContent()
{
    // Configure EF Core with PostgreSQL
    string projectId = builder.Configuration.GetSection("Gcp").GetValue<string>("ProjectID") ?? throw new ArgumentNullException("Gcp Project ID not configured.");
    string secretId = builder.Configuration.GetSection("Gcp").GetValue<string>("SecretID") ?? throw new ArgumentNullException("Gcp Secret ID not configured.");
    string secretVersion = builder.Configuration.GetSection("Gcp").GetValue<string>("SecretVersion") ?? throw new ArgumentNullException("Gcp Secret Version not configured.");

    //Init Secret Manager Client
    SecretManagerServiceClient client = SecretManagerServiceClient.Create();

    //get the secret value for database connection
    SecretVersionName secretVersionName = new(projectId, secretId, secretVersion);
    AccessSecretVersionResponse result = client.AccessSecretVersion(secretVersionName);
    string certContent = result.Payload.Data.ToStringUtf8();
    return certContent;
}

void ApplyMigrations()
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (dbContext.Database.GetPendingMigrations().Any())
        {
            dbContext.Database.Migrate();
        }
    }
    ;

}