/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/aspnet-contrib/AspNet.Security.OAuth.Providers
 * for more information concerning the license and the contributors participating to this project.
 */
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace AspNet.Security.OAuth.Strava
{
    /// <summary>
    /// Defines a set of options used by <see cref="StravaAuthenticationHandler"/>.
    /// </summary>
    public class StravaAuthenticationOptions : OAuthOptions
    {
        public StravaAuthenticationOptions()
        {
            ClaimsIssuer = StravaAuthenticationDefaults.Issuer;
            CallbackPath = new PathString(StravaAuthenticationDefaults.CallbackPath);
            AuthorizationEndpoint = StravaAuthenticationDefaults.AuthorizationEndpoint;
            TokenEndpoint = StravaAuthenticationDefaults.TokenEndpoint;
            UserInformationEndpoint = StravaAuthenticationDefaults.UserInformationEndpoint;
            CallbackPath = new PathString(StravaAuthenticationDefaults.CallbackPath);
            ClaimsIssuer = StravaAuthenticationDefaults.Issuer;
            Scope.Add(StravaAuthenticationDefaults.Scope);
            // Adding claims
            Events.OnCreatingTicket = async context =>
            {
                // Call out to user information endpoint with the access token
                var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", context.AccessToken);

                var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException("An error occurred while retrieving the user profile");
                }

                // Deserialise response with user data payload
                var payload = JObject.Parse(await response.Content.ReadAsStringAsync());

                // Add claims to identity
                context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, StravaAuthenticationHelper.GetIdentifier(payload), ClaimValueTypes.Integer));
                context.Identity.AddClaim(new Claim(ClaimTypes.Name, StravaAuthenticationHelper.GetUsername(payload)));
                context.Identity.AddClaim(new Claim(ClaimTypes.GivenName, StravaAuthenticationHelper.GetFirstName(payload)));
                context.Identity.AddClaim(new Claim(ClaimTypes.Surname, StravaAuthenticationHelper.GetLastName(payload)));
                context.Identity.AddClaim(new Claim(ClaimTypes.Email, StravaAuthenticationHelper.GetEmail(payload)));
                context.Identity.AddClaim(new Claim(ClaimTypes.StateOrProvince, StravaAuthenticationHelper.GetState(payload)));
                context.Identity.AddClaim(new Claim(ClaimTypes.Country, StravaAuthenticationHelper.GetCountry(payload)));
                context.Identity.AddClaim(new Claim(ClaimTypes.Gender, StravaAuthenticationHelper.GetGender(payload)));
                context.Identity.AddClaim(new Claim("urn:strava:city", StravaAuthenticationHelper.GetCity(payload)));
                context.Identity.AddClaim(new Claim("urn:strava:accessToken", context.AccessToken));
                context.Identity.AddClaim(new Claim("urn:strava:profile", StravaAuthenticationHelper.GetProfileImage(payload)));
                context.Identity.AddClaim(new Claim("urn:strava:profile-medium", StravaAuthenticationHelper.GetProfileImageMedium(payload)));
                context.Identity.AddClaim(new Claim("urn:strava:premium", StravaAuthenticationHelper.GetIsPremium(payload)));
                context.Identity.AddClaim(new Claim("urn:strava:created-at", StravaAuthenticationHelper.GetCreatedDate(payload)));
                context.Identity.AddClaim(new Claim("urn:strava:updated-at", StravaAuthenticationHelper.GetLastUpdatedDate(payload)));
            };
        }
    }
}
