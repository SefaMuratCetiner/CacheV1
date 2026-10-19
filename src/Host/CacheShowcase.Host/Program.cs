using CacheShowcase.Host.Observability;
using CacheShowcase.Modules.Analytics;
using CacheShowcase.Modules.Carts;
using CacheShowcase.Modules.Catalog;
using CacheShowcase.Modules.Customers;
using CacheShowcase.Modules.Inventory;
using CacheShowcase.Modules.Pricing;
using CacheShowcase.Modules.ReferenceData;
using CacheShowcase.Modules.Reports;
using FastEndpoints;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

builder.AddObservability();

builder.Services
    // Building blocks
    .AddPostgres(configuration)
    .AddCaching(configuration)
    .AddHttpCaching()
    // Modules
    .AddCatalogModule()
    .AddCustomersModule()
    .AddPricingModule(configuration)
    .AddInventoryModule()
    .AddCartsModule()
    .AddAnalyticsModule()
    .AddReportsModule(configuration)
    .AddReferenceDataModule()
    // Endpoints, validators and event handlers are discovered in the module assemblies.
    .AddFastEndpoints(options => options.Assemblies =
    [
        CatalogModule.Assembly,
        CustomersModule.Assembly,
        PricingModule.Assembly,
        InventoryModule.Assembly,
        CartsModule.Assembly,
        AnalyticsModule.Assembly,
        ReportsModule.Assembly,
        ReferenceDataModule.Assembly,
    ]);

var app = builder.Build();

app.UseDefaultExceptionHandler(useProblemDetails: true);
app.UseHttpCaching();
app.UseFastEndpoints(config =>
{
    config.Endpoints.RoutePrefix = "api";

    // No authentication in this project: every endpoint is public.
    config.Endpoints.Configurator = endpoint => endpoint.AllowAnonymous();
    config.Errors.UseProblemDetails();
});

await app.RunAsync();
