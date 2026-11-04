using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace DriveHub.Controllers;

public class AccountController : Controller
{
    private readonly FirebaseAuth _firebaseAuth;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public AccountController(
        FirebaseApp firebaseApp,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _firebaseAuth = FirebaseAuth.GetAuth(firebaseApp);
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    // REGISTER - GET
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    // REGISTER - POST
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            await _firebaseAuth.CreateUserAsync(new UserRecordArgs
            {
                Email = model.Email,
                Password = model.Password,
                DisplayName = model.FullName
            });

            TempData["Success"] =
                "Account created successfully! Please sign in.";

            return RedirectToAction(nameof(Login));
        }
        catch (FirebaseAuthException)
        {
            ModelState.AddModelError(
                "",
                "Unable to create account. Check your details or try another email.");

            return View(model);
        }
    }

    // LOGIN - GET
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login()
    {
        return View(new LoginViewModel());
    }

    // LOGIN - POST
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var apiKey = _configuration["Firebase:WebApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "Firebase:WebApiKey is not configured.");
        }

        var client = _httpClientFactory.CreateClient();

        var url =
            "https://identitytoolkit.googleapis.com/v1/" +
            $"accounts:signInWithPassword?key={Uri.EscapeDataString(apiKey)}";

        HttpResponseMessage response;

        try
        {
            response = await client.PostAsJsonAsync(url, new
            {
                email = model.Email,
                password = model.Password,
                returnSecureToken = true
            });
        }
        catch (HttpRequestException)
        {
            ModelState.AddModelError(
                "",
                "Authentication service is unavailable. Try again.");

            return View(model);
        }
        catch (TaskCanceledException)
        {
            ModelState.AddModelError(
                "",
                "Authentication service timed out. Try again.");

            return View(model);
        }

        // Keep the response alive until its content has been read.
        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to sign in. Please check your email and password.");

                return View(model);
            }

            FirebaseLoginResponse? result;

            try
            {
                result = await response.Content
                    .ReadFromJsonAsync<FirebaseLoginResponse>();
            }
            catch (System.Text.Json.JsonException)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid response from authentication service.");

                return View(model);
            }

            if (string.IsNullOrWhiteSpace(result?.IdToken))
            {
                ModelState.AddModelError(
                    "",
                    "Unable to verify your login.");

                return View(model);
            }

            // Verify the Firebase ID token server-side.
            FirebaseToken verifiedToken;

            try
            {
                verifiedToken = await _firebaseAuth
                    .VerifyIdTokenAsync(
                        result.IdToken,
                        checkRevoked: true);
            }
            catch (FirebaseAuthException)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to verify your login.");

                return View(model);
            }

            // Retrieve the verified user from Firebase Admin.
            UserRecord firebaseUser;

            try
            {
                firebaseUser = await _firebaseAuth
                    .GetUserAsync(verifiedToken.Uid);
            }
            catch (FirebaseAuthException)
            {
                ModelState.AddModelError(
                    "",
                    "Unable to retrieve your account.");

                return View(model);
            }

            if (firebaseUser.Disabled)
            {
                ModelState.AddModelError(
                    "",
                    "This account is disabled.");

                return View(model);
            }

            // Build the authenticated ASP.NET Core identity.
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    verifiedToken.Uid),

                new Claim(
                    ClaimTypes.Name,
                    firebaseUser.DisplayName ??
                    firebaseUser.Email ??
                    "Drive Hub User"),

                new Claim(
                    ClaimTypes.Email,
                    firebaseUser.Email ?? model.Email)
            };

            // Roles are read from trusted Firebase Admin data,
            // never from a registration or login form field.
            if (firebaseUser.CustomClaims != null &&
                firebaseUser.CustomClaims.TryGetValue(
                    "role",
                    out var role) &&
                role?.ToString() == "Seller")
            {
                claims.Add(
                    new Claim(ClaimTypes.Role, "Seller"));
            }
            else
            {
                claims.Add(
                    new Claim(ClaimTypes.Role, "Buyer"));
            }

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            // Sign in with the existing two-hour cookie.
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
                });

            return RedirectToAction("Index", "Parts");
        }
    }

    // LOGOUT - POST
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return RedirectToAction(nameof(Login));
    }

    // ACCESS DENIED
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}

// REGISTRATION FORM MODEL
public class RegisterViewModel
{
    [Required]
    public string FullName { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [MinLength(8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = "";
}

// LOGIN FORM MODEL
public class LoginViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";
}

// FIREBASE IDENTITY TOOLKIT RESPONSE
public class FirebaseLoginResponse
{
    [JsonPropertyName("idToken")]
    public string IdToken { get; set; } = "";
}
