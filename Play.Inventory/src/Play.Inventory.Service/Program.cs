using Play.Common.MongoDB;
using Play.Common.Settings;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Entities;
using Polly;
using Polly.Timeout;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var serviceSettings = builder.Configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();

builder.Services.AddMongo()
        .AddMongoRepository<InventoryItem>("inventoryitems");

Random jitterer = new Random();

builder.Services.AddHttpClient<CatalogClient>(client =>
{
    client.BaseAddress = new Uri(serviceSettings.CatalogBaseUrl);
})
.AddTransientHttpErrorPolicy(p =>
    p.Or<TimeoutRejectedException>().WaitAndRetryAsync(5,
     _ => TimeSpan.FromSeconds(Math.Pow(2, _)) + TimeSpan.FromMilliseconds(jitterer.Next(0, 1000)),
    onRetry: (outcome, timespan, retryCount, context) =>
    {
        var serviceProvider = builder.Services.BuildServiceProvider();
        serviceProvider.GetService<ILogger<CatalogClient>>().LogWarning($"Delaying for {timespan.TotalSeconds} seconds, then making retry {retryCount}");
    }))
.AddTransientHttpErrorPolicy(p => p.Or<TimeoutRejectedException>().CircuitBreakerAsync(3, TimeSpan.FromSeconds(15), onBreak: (outcome, timespan) =>
{
    var serviceProvider = builder.Services.BuildServiceProvider();
    serviceProvider.GetService<ILogger<CatalogClient>>().LogWarning($"Opening the circuit for {timespan.TotalSeconds} seconds...");
}, onReset: () =>
{
    var serviceProvider = builder.Services.BuildServiceProvider();
    serviceProvider.GetService<ILogger<CatalogClient>>().LogWarning("Closing the circuit...");
}))
.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(1));
 
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

app.MapControllers();
app.UseHttpsRedirection();

app.Run();
