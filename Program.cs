using DriveHub.Services;
using Google.Cloud.Firestore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
var projectId = builder.Configuration["Firebase:ProjectId"];
if (string.IsNullOrWhiteSpace(projectId))
    throw new InvalidOperationException("Set Firebase:ProjectId using dotnet user-secrets before running.");
builder.Services.AddSingleton(FirestoreDb.Create(projectId));
builder.Services.AddSingleton<PartRepository>();
var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Parts/Index"); app.UseHsts(); }
app.UseHttpsRedirection(); app.UseStaticFiles(); app.UseRouting();
app.MapControllerRoute("default", "{controller=Parts}/{action=Index}/{id?}");
app.Run();

Console.WriteLine(
    $"Environment: {builder.Environment.EnvironmentName}");

Console.WriteLine(
    $"Firebase Project ID configured: " +
    $"{!string.IsNullOrWhiteSpace(builder.Configuration["Firebase:ProjectId"])}");