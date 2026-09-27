using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using KriyaTracker.Infrastructure.Data;
using KriyaTracker.Domain.Interfaces;
using KriyaTracker.Application.ViewModels;
using System.IO;

namespace KriyaTracker.UI;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Database setup
		string dbPath = Path.Combine(FileSystem.AppDataDirectory, "kriyatracker.db3");
		builder.Services.AddDbContext<AppDbContext>(options =>
			options.UseSqlite($"Filename={dbPath}"));

		// Repositories & Services
		builder.Services.AddScoped<ISessionRepository, SessionRepository>();
		builder.Services.AddSingleton<IWatchSyncService, KriyaTracker.Infrastructure.WearOS.WearOsSyncService>();

		// ViewModels
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<SessionViewModel>();
		
		// Pages
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<SessionPage>();

		return builder.Build();
	}
}
