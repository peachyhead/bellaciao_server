using Microsoft.EntityFrameworkCore;
using Context;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddDbContext<MyClassroomContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader()
          .WithExposedHeaders("Content-Disposition"));
});

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Включаем CORS перед обработкой запросов
app.UseCors("AllowAll");
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run("http://0.0.0.0:5000");