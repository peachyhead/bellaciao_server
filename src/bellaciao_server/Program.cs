using Microsoft.EntityFrameworkCore;
using Context;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000, listenOptions =>
    {
        listenOptions.UseHttps("/home/acid_admin/myclassroom.pfx", "-1usedBan");
    });
});

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
app.UseHttpsRedirection();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.Run("https://0.0.0.0:5000");