using AuthenticationApi.Configurations;
using AuthenticationData.Data;
using AuthenticationData.Services;
using AuthenticationServices.Services.Implementation;
using AuthenticationServices.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

if (builder.Environment.IsEnvironment("Development"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}
else
{
    var server = builder.Configuration["DatabaseServer"];
    var port = builder.Configuration["DatabasePort"];
    var user = builder.Configuration["DatabaseUser"];
    var password = builder.Configuration["DatabasePassword"];
    var database = builder.Configuration["DatabaseName"];
    var connectionString = $"Server={server},{port};Initial Catalog={database};User ID={user};Password={password}";

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString));
}

builder.Services.AddIdentityCore<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<AppDbContext>();

builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JwtConfig"));

var key = Encoding.ASCII.GetBytes(builder.Configuration["JwtConfig:Secret"]);

var validationParams = new TokenValidationParameters
{
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = builder.Configuration["JwtConfig:Issuer"],
    ValidAudience = builder.Configuration["JwtConfig:Audience"],
    IssuerSigningKey = new SymmetricSecurityKey(key)
};

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.TokenValidationParameters = validationParams;
});

builder.Services.AddSingleton(validationParams);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{ 
    DatabaseManagementService.MigrationInitialization(app);
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
