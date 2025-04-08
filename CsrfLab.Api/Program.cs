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
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; 
    });


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


app.UseCors("AllowAngularApp");
app.UseAuthentication();
app.UseAuthorization();


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

app.MapPost("/pay", [Authorize] async (HttpContext http) =>
{
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

app.UseStaticFiles(); 

app.Run();

record PaymentRequest(string To, decimal Amount);

