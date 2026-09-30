using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Extensions.Hosting;

new HostBuilder()
	.ConfigureFunctionsWorkerDefaults()
	.ConfigureOpenApi()
	.Build()
	.Run();
