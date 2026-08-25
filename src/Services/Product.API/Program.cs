using Common.Observability;

var builder = WebApplication.CreateBuilder(args);

// 註冊 OpenTelemetry 全鏈路可觀測性
builder.Services.AddCustomObservability(builder.Configuration, "ProductService");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", service = "ProductService" })).AllowAnonymous();

app.Run();
