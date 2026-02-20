using ChatSupportSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register application services as singletons (in-memory state)
builder.Services.AddSingleton<TeamConfigurationService>();
builder.Services.AddSingleton<ChatQueue>();
builder.Services.AddSingleton<ShiftManager>();
builder.Services.AddSingleton<ChatAssignmentService>();
builder.Services.AddSingleton<ChatCoordinator>();

// Background service for monitoring
builder.Services.AddHostedService<QueueMonitorService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
