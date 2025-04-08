using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200") 
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); 
    });
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.Cookie.Name = "csrf-demo-auth";
        options.Cookie.SameSite = SameSiteMode.None; // CORRECAO 1 - ADICIONANDO O STRICT, BLOQUEIA TUDO 
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; 
    });

//PASSO 1 - CRIAR O SERVIÇO DE ANTICSRF
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN"; // nome do header que o Angular enviará
    options.Cookie.Name = "XSRF-TOKEN";  // nome do cookie que o Angular irá ler
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.HttpOnly = false; // ← Permite que o Angular leia
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // ou Always com HTTPS
});

//PASSO 2 - CRIAR O METODO QUE RETORNA O TOKEN


builder.Services.AddAuthorization();

var app = builder.Build();

var accounts = new Dictionary<string, decimal>
{
    { "Mr. Smith", 1000m }
};


app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Access-Control-Allow-Credentials", "true");
    await next.Invoke();
});


//DEPOIS DO PASSO 3 E TUDO NO PROJETO DO ANGULAR

app.UseCors("AllowAngularApp");
app.UseAuthentication();
app.UseAuthorization();

// ESSE CARA AQUI! PASSO 2
// O 3 E ATUALIZAR O /PAY
app.MapGet("/antiforgery-token", (IAntiforgery antiforgery, HttpContext context) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);

    // Importante: enviar o RequestToken de volta no corpo para o Angular
    return Results.Ok(new
    {
        requestToken = tokens.RequestToken
    });
});

app.MapGet("/balance", [Authorize] (HttpContext http) =>
{
    var user = http.User.Identity?.Name!;
    if (!accounts.ContainsKey(user))
        return Results.NotFound("Conta não encontrada.");

    return Results.Ok(new { balance = accounts[user] });
});


app.MapPost("/login", async (HttpContext http) =>
{
    var claims = new[] { new Claim(ClaimTypes.Name, "Mr. Smith") };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

    return Results.Ok("Logado como Mr. Smith");
});

//PASSO 3 AQUI
app.MapPost("/pay", [Authorize] async (HttpContext http, IAntiforgery antiforgery) =>
{
    await antiforgery.ValidateRequestAsync(http);
    string to;
    decimal amount;

    if (http.Request.HasFormContentType)
    {
        var form = await http.Request.ReadFormAsync();
        to = form["to"]!;
        amount = decimal.Parse(form["amount"]!);
    }
    else
    {
        var payment = await http.Request.ReadFromJsonAsync<PaymentRequest>();
        if (payment == null) return Results.BadRequest("Dados inválidos.");
        to = payment.To;
        amount = payment.Amount;
    }

    var user = http.User.Identity?.Name;

    if (user is null || !accounts.ContainsKey(user))
        return Results.Unauthorized();

    if (accounts[user] < amount)
        return Results.BadRequest("Saldo insuficiente.");

    accounts[user] -= amount;

    Console.WriteLine($"[💸 CSRF ou legítimo] {user} → {to}: R${amount}");

    return Results.Ok(new
    {
        Message = "Transferência realizada",
        NewBalance = accounts[user]
    });
});



app.Run();

record PaymentRequest(string To, decimal Amount);

