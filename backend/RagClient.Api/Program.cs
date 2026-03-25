using Microsoft.Extensions.Options;
using RagClient.Api.Configuration;
using RagClient.Api.Infrastructure;
using RagClient.Api.Middleware;
using RagClient.Api.Models;
using RagClient.Api.Services;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RagApiOptions>(
    builder.Configuration.GetSection(RagApiOptions.SectionName));

builder.Services.AddScoped<ApiKeyContext>();

builder.Services.AddHttpClient<IRagApiClient, RagApiClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<RagApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(120);
});

builder.Services.AddScoped<ICollectionService, CollectionService>();
builder.Services.AddScoped<IRagService, RagService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseCors();
app.UseMiddleware<ApiKeyMiddleware>();
app.MapControllers();

app.Run();
