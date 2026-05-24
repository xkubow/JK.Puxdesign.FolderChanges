using JK.Puxdesign.FolderChanges;
using JK.Puxdesign.FolderChanges.Configurations;
using JK.Puxdesign.FolderChanges.Repositories;
using JK.Puxdesign.FolderChanges.Services;
using JK.Puxdesign.FolderChanges.Store;
using MudBlazor.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // Add services to the container.
    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    builder.Services.Configure<FolderSettings>(
        builder.Configuration.GetSection("FolderSettings"));

    builder.Services.AddMudServices();
    builder.Services.AddSingleton<IFolderStateStore, FolderStateStore>();
    builder.Services.AddTransient<IFolderStateFileStore, FolderStateFileStore>();
    builder.Services.AddTransient<IFolderRepository, FolderRepository>();
    builder.Services.AddTransient<IFolderService, FolderService>();

    builder.WebHost.UseStaticWebAssets();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
        app.UseHttpsRedirection();
    }


    app.MapStaticAssets();
    app.UseAntiforgery();

    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
