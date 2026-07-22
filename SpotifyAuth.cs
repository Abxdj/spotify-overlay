using System.IO;
using System.Text.Json;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace SpotifyOverlay;

public static class SpotifyAuth
{
    // ---- PUT YOUR CLIENT ID HERE ----
    private const string ClientId = "f4e42bf1509a4bf58eeb2729149998fa";
    private const int    CallbackPort = 5543;
    private static readonly Uri Callback = new($"http://127.0.0.1:{CallbackPort}/callback");

    private static readonly string DataDir =
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SpotifyOverlay");

    private static readonly string TokenPath = Path.Combine(DataDir, "token.json");

    private static readonly List<string> RequiredScopes = new()
    {
        Scopes.UserReadPlaybackState,
        Scopes.UserModifyPlaybackState,
        Scopes.UserReadCurrentlyPlaying
    };

    /// <summary>Returns an authenticated SpotifyClient, prompting login only if needed.</summary>
    public static async Task<SpotifyClient> GetClientAsync()
    {
        var token = LoadToken();

        if (token == null)
        {
            token = await RunLoginFlowAsync();
            SaveToken(token);
        }

        var config = SpotifyClientConfig
            .CreateDefault()
            .WithAuthenticator(new PKCEAuthenticator(ClientId, token));

        // Persist refreshed tokens automatically
        var auth = (PKCEAuthenticator)config.Authenticator!;
        auth.TokenRefreshed += (_, t) => SaveToken(t);

        return new SpotifyClient(config);
    }

    private static async Task<PKCETokenResponse> RunLoginFlowAsync()
    {
        var (verifier, challenge) = PKCEUtil.GenerateCodes();

        var tcs = new TaskCompletionSource<PKCETokenResponse>();
        var server = new EmbedIOAuthServer(Callback, CallbackPort);
        await server.Start();

        server.AuthorizationCodeReceived += async (_, response) =>
        {
            await server.Stop();
            var token = await new OAuthClient().RequestToken(
                new PKCETokenRequest(ClientId, response.Code, Callback, verifier));
            tcs.SetResult(token);
        };

        var login = new LoginRequest(Callback, ClientId, LoginRequest.ResponseType.Code)
        {
            CodeChallengeMethod = "S256",
            CodeChallenge = challenge,
            Scope = RequiredScopes
        };

        var uri = login.ToUri();
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = uri.ToString(),
            UseShellExecute = true
        });
        return await tcs.Task;
    }

    private static PKCETokenResponse? LoadToken()
    {
        if (!File.Exists(TokenPath)) return null;
        try { return JsonSerializer.Deserialize<PKCETokenResponse>(File.ReadAllText(TokenPath)); }
        catch { return null; }
    }

    private static void SaveToken(PKCETokenResponse token)
    {
        Directory.CreateDirectory(DataDir);
        File.WriteAllText(TokenPath, JsonSerializer.Serialize(token));
    }
}