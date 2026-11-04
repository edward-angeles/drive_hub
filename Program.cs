using DriveHub.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// MVC controllers and Razor views
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

// Firebase project configuration
var projectId = builder.Configuration["Firebase:ProjectId"];

if (string.IsNullOrWhiteSpace(projectId))
{
    throw new InvalidOperationException(
        "Set Firebase:ProjectId using dotnet user-secrets before running.");
}

// Existing Firebase Firestore connection
builder.Services.AddSingleton(FirestoreDb.Create(projectId));
builder.Services.AddSingleton<PartRepository>();

// Firebase Admin SDK
builder.Services.AddSingleton(provider =>
{
    return FirebaseApp.DefaultInstance ??
        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.GetApplicationDefault(),
            ProjectId = projectId
        });
});

// ASP.NET Core cookie authentication
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";

        options.Cookie.Name = "DriveHub.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
    });

// Authorization services
builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Parts/Index");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Authentication must run before authorization
app.UseAuthentication();
app.UseAuthorization();

// Default homepage remains the buyer marketplace
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Parts}/{action=Index}/{id?}");

Console.WriteLine(
    $"Environment: {app.Environment.EnvironmentName}");

Console.WriteLine(
    $"Firebase Project ID configured: {!string.IsNullOrWhiteSpace(projectId)}");

app.Run();