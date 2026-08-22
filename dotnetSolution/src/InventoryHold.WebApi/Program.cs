using InventoryHold.WebApi.Configurations;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services
    .AddRedisDependencies(builder.Configuration)
    .AddRabbitMqDependencies(builder.Configuration)
    .AddMongoDependencies(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory Hold API v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
