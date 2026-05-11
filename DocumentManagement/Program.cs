using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DocumentManagement.Authorization;
using DocumentManagement.Services;

var builder = WebApplication.CreateBuilder(args);

const string jwtIssuer = "DocumentManagement.Local";
const string jwtAudience = "DocumentManagement.Api";
const string jwtSigningKey = "DocumentManagement_Local_JWT_Signing_Key_2026!";

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
    .AddPolicy(AuthorizationPolicies.ReadPolicy,   policy => policy.RequireClaim("permission", AppPermissions.Read))
    .AddPolicy(AuthorizationPolicies.CreatePolicy, policy => policy.RequireClaim("permission", AppPermissions.Create))
    .AddPolicy(AuthorizationPolicies.UpdatePolicy, policy => policy.RequireClaim("permission", AppPermissions.Update))
    .AddPolicy(AuthorizationPolicies.DeletePolicy, policy => policy.RequireClaim("permission", AppPermissions.Delete));

builder.Services.AddSingleton(new JwtTokenOptions(jwtIssuer, jwtAudience, jwtSigningKey));
builder.Services.AddSingleton<DocumentStore>();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Document Management API",
        Version = "v1",
        Description = "Document management API with JWT authentication, role-based authorization and per-document ownership."
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
