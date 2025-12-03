using iTarlaMapBackend.Models;
using iTarlaMapBackend.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure DB settings using Options pattern
builder.Services.Configure<iTarlaDbConnection>(
    builder.Configuration.GetSection("iTarlaDBSettings")
);
// Add controllers
builder.Services.AddControllers();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register your service (it will get IOptions<iTarlaDbConnection> automatically)
builder.Services.AddSingleton<FarmerService>();
builder.Services.AddSingleton<FarmService>();
var app = builder.Build();

// Enable Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
