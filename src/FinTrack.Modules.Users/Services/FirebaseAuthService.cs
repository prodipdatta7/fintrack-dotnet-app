using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;

namespace FinTrack.Modules.Users.Services;

/// <summary>
/// Thin wrapper over the Firebase Admin SDK used for server-side auth operations (revoke
/// sessions, delete users). The Firebase app is created lazily with a service-account
/// credential supplied via <c>Firebase:CredentialJson</c>, <c>Firebase:CredentialPath</c>, or
/// <c>GOOGLE_APPLICATION_CREDENTIALS</c>.
/// </summary>
public sealed class FirebaseAuthService : IFirebaseAuthService
{
    private const string AppName = "fintrack-admin";

    private static readonly object AppLock = new();

    private readonly IConfiguration _configuration;

    public FirebaseAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task RevokeRefreshTokensAsync(string firebaseUid, CancellationToken cancellationToken)
    {
        var auth = FirebaseAuth.GetAuth(GetOrCreateApp());
        await auth.RevokeRefreshTokensAsync(firebaseUid, cancellationToken);
    }

    public async Task DeleteUserAsync(string firebaseUid, CancellationToken cancellationToken)
    {
        var auth = FirebaseAuth.GetAuth(GetOrCreateApp());
        try
        {
            await auth.DeleteUserAsync(firebaseUid, cancellationToken);
        }
        catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
        {
            // Already deleted — treat as success so account cleanup can continue.
        }
    }

    private FirebaseApp GetOrCreateApp()
    {
        try
        {
            return FirebaseApp.GetInstance(AppName);
        }
        catch (InvalidOperationException)
        {
            lock (AppLock)
            {
                try
                {
                    return FirebaseApp.GetInstance(AppName);
                }
                catch (InvalidOperationException)
                {
                    return FirebaseApp.Create(BuildOptions(), AppName);
                }
            }
        }
    }

    private AppOptions BuildOptions()
    {
        var projectId = _configuration["Firebase:ProjectId"];

        var credentialJson = _configuration["Firebase:CredentialJson"];
        var credentialPath = _configuration["Firebase:CredentialPath"]
            ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");

        GoogleCredential credential;
        if (!string.IsNullOrWhiteSpace(credentialJson))
        {
            credential = CredentialFactory
                .FromJson<ServiceAccountCredential>(credentialJson)
                .ToGoogleCredential();
        }
        else if (!string.IsNullOrWhiteSpace(credentialPath) && File.Exists(credentialPath))
        {
            credential = CredentialFactory
                .FromFile<ServiceAccountCredential>(credentialPath)
                .ToGoogleCredential();
        }
        else
        {
            credential = GoogleCredential.GetApplicationDefault();
        }

        return new AppOptions
        {
            ProjectId = projectId,
            Credential = credential
        };
    }
}
