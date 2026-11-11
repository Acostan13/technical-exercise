using Microsoft.EntityFrameworkCore;
using StargateAPI.Business;
using StargateAPI.Business.Data;
using StargateAPI.Infrastructure;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Enums (e.g. process log level) are returned as readable names rather than numbers.
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Unreadable request bodies use the same response envelope as every other error.
        options.InvalidModelStateResponseFactory = InvalidRequestResponseFactory.Create;
    });

builder.Services.AddExceptionHandler<StargateExceptionHandler>();
builder.Services.AddProblemDetails();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlDocs = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    options.IncludeXmlComments(xmlDocs);
});

var connectionString = builder.Configuration.GetConnectionString("StarbaseApiDatabase")
    ?? throw new InvalidOperationException("Connection string 'StarbaseApiDatabase' is not configured.");

builder.Services.AddDbContext<StargateContext>(options => options.UseSqlite(connectionString));

builder.Services.AddStargateBusiness();

var app = builder.Build();

// Create the database and apply any pending migrations so the API is runnable from a clean checkout.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<StargateContext>().Database.Migrate();
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

/// <summary>Exposed so integration tests can host the application with WebApplicationFactory.</summary>
public partial class Program
{
}
