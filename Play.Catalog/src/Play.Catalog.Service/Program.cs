using Play.Catalog.Service.Entities;
using Play.Common.MongoDB;
using Play.Common.Settings;
using MassTransit;
using Play.Common.MassTransit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var serviceSettings = builder.Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();

builder.Services.AddMongo()
        .AddMongoRepository<Item>("items")
        .AddMassTransitWithRabbitMq();


builder.Services.AddControllers(options =>
{
    options.SuppressAsyncSuffixInActionNames = false;
});


var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(cors => cors.WithOrigins(builder.Configuration["AllowedOrigins"])
    .AllowAnyHeader()
    .AllowAnyMethod());

app.MapControllers();
app.UseHttpsRedirection();

app.Run();
