using System;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public BasicAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock) : base(options, logger, encoder, clock)
    {
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        return await Task.Run<AuthenticateResult>(() =>
        {
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                return AuthenticateResult.Fail("Missing Authorization header");
            }

            try
            {
                var auth = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);


                if (auth.Scheme != "Basic")
                {
                    return AuthenticateResult.Fail("Invalid Authorization scheme");
                }

                var credentialBytes = Convert.FromBase64String(auth.Parameter ?? "");
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);

                var username = credentials[0];
                var password = credentials[1];

                // You would normally check the credentials against a database or other
                // authentication source here. For demonstration purposes, we're just
                // checking against a hardcoded username and password.

                if (username == "76x8h9rPcsnAb7VTFXZAsg687JutpAHTpmg4xWNWpGQx75rARg8Ak7JeCt46tYcg6rNnZJfjXe7MW8mJ9Nc2sFJJ9ch5vUZJQmTsrWtsZm97QdD2bL5aprtdPtC6C9Vjtccg4VAbMtgcKVWANyvhkpgN3fwjNmMjfcRatbTFcAr29Mm8wppQgyKz4r5ztfSxgPjqwM6TTRH9VCtjaJ7grkZCzrMbnxWDsFjdEBAQEMMRhp9EYbdTYtMArevHKKAEA3HUX7kj6LpU7B6r3nvANnuVPBapmbDC7fK8uAkRFHNtPC85fFQXr2bSTsLJXUFUswQqDyKKndJDjTyMnV9dNNeABruMkALqan9tEq3vmju3LK2rTuQXxvayr24sNzEesTqTbDsmVytYCcDMspdBxMYVZCBm6ECmzhKS6jWE23TazFEDshFqC429s97LuyaWjAGGQe3dgvPwmceFTWjxjqK68hZ8uGcn57j5LNsNCvUdWCFUcp4E9LB4EfD62YbA")
                {
                    var claims = new[] {
                    new Claim(ClaimTypes.NameIdentifier, username),
                    new Claim(ClaimTypes.Name, username)
                };

                    var identity = new ClaimsIdentity(claims, Scheme.Name);
                    var principal = new ClaimsPrincipal(identity);
                    var ticket = new AuthenticationTicket(principal, Scheme.Name);

                    return AuthenticateResult.Success(ticket);
                }

                return AuthenticateResult.Fail("Invalid username or password");
            }
            catch
            {
                return AuthenticateResult.Fail("Invalid Authorization header");
            }

        });
    }
}
