using Api;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

new HostBuilder()
	.ConfigureFunctionsWorkerDefaults()
	.ConfigureOpenApi()
	.ConfigureServices(services => services.AddSingleton<ICalendarRepository, FileCalendarRepository>())
	.Build()
	.Run();
