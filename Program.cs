using KingdomHeartsDle.Components;
using KingdomHeartsDle.Services;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Traductions fr/en, puis chargement des modes de jeu (Data/*.json) une seule fois au démarrage.
builder.Services.AddSingleton<I18n>();
builder.Services.AddSingleton<GameService>();
builder.Services.AddSingleton<ImageService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

// Les images changent rarement : en production, le navigateur les garde 7 jours (pas de cache en développement).
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (!app.Environment.IsDevelopment() && ctx.Context.Request.Path.StartsWithSegments("/images"))
            ctx.Context.Response.Headers.CacheControl = "public,max-age=604800";
    }
});

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

// Développement uniquement : liste, par mode, les images trouvées et les fichiers attendus pour les manquantes.
if (app.Environment.IsDevelopment())
{
    app.MapGet("/dev/images", (ImageService images) => Results.Json(images.Report()));
}

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Services.GetRequiredService<ImageService>().LogSummary();

app.Run();
