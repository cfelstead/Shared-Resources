using McpSidecarServer.Services;
using McpSidecarServer.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient();
builder.Services.AddSingleton<GeocodingService>();
builder.Services.AddSingleton<OpenMeteoService>();
builder.Services.AddSingleton<DateTimeTool>();
builder.Services.AddSingleton<WeatherTool>();

builder.Services
	.AddMcpServer()
	.WithStdioServerTransport()
	.WithTools<DateTimeTool>()
	.WithTools<WeatherTool>();

await builder.Build().RunAsync();
