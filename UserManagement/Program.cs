using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using UserManagement.Authorization;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

const string jwtIssuer = "UserManagement.Local";
const string jwtAudience = "UserManagement.Api";
const string jwtSigningKey = "UserManagement_Local_JWT_Signing_Key_2026!";

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.ReadPolicy, policy => policy.RequireRole(AuthorizationPolicies.ReadRole))
    .AddPolicy(AuthorizationPolicies.CreatePolicy, policy => policy.RequireRole(AuthorizationPolicies.CreateRole))
    .AddPolicy(AuthorizationPolicies.UpdatePolicy, policy => policy.RequireRole(AuthorizationPolicies.UpdateRole))
    .AddPolicy(AuthorizationPolicies.DeletePolicy, policy => policy.RequireRole(AuthorizationPolicies.DeleteRole));

builder.Services.AddSingleton(new JwtTokenOptions(jwtIssuer, jwtAudience, jwtSigningKey));
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "User Management API",
        Version = "v1",
        Description = "Local user management API with JWT authentication and role-based authorization."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter your JWT token (without the 'Bearer' prefix)"
    });

    options.OperationFilter<BearerSecurityOperationFilter>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
