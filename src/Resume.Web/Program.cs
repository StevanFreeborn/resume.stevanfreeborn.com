using Resume.Web.Endpoints;
using Resume.Web.Extensions;
using Resume.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddResumeWeb();

var app = builder.Build();

app.UseForwardedHeaders();
app.UsePathBase("/resume");
app.UseResumeErrorPages();
app.UseRouting();
app.MapResumeEndpoints();

await app.RunAsync();
