using KingdomHeartsDle.Components;
using KingdomHeartsDle.Services;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Traductions fr/en, puis chargement des modes de jeu (Data/*.json) une seule fois au démarrage.
builder.Services.AddSingleton<I18n>();
builder.Services.AddSingleton<GameService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();

// Langue : cookie posé par /culture/{code}, sinon en-tête Accept-Language, sinon français.
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("fr")
    .AddSupportedCultures(I18n.SupportedLanguages)
    .AddSupportedUICultures(I18n.SupportedLanguages));

app.UseAntiforgery();

app.MapGet("/culture/{code}", (string code, string? returnUrl, HttpContext http) =>
{
    var lang = code == "en" ? "en" : "fr";
    http.Response.Cookies.Append(
        CookieRequestCultureProvider.DefaultCookieName,
        CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(lang)),
        new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true, SameSite = SameSiteMode.Lax });

    return Results.LocalRedirect(!string.IsNullOrEmpty(returnUrl) && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative) ? returnUrl : "/");
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
