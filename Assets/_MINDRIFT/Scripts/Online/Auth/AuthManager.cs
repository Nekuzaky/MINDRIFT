using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Mindrift.Auth;
using Mindrift.Online.Core;
using Mindrift.Online.Models;
using UnityEngine;

namespace Mindrift.Online.Auth
{
    public sealed class AuthManager : IAuthService
    {
        private static AuthManager instance;

        private readonly ApiClient apiClient;
        private AuthSessionData currentSession;
        private UserSummary currentUser;
        private Task<AuthSessionData> restoreSessionTask;
        private bool hasAttemptedRestore;

        public static AuthManager Instance => instance ??= new AuthManager();

        public event Action<AuthSessionData> SessionChanged;

        public AuthSessionData CurrentSession => currentSession ??= AuthSessionData.CreateGuest();
        public UserSummary CurrentUser => currentUser;

        private AuthManager()
        {
            apiClient = new ApiClient(ApiConfig.Active);
            currentSession = AuthSessionData.CreateGuest();
        }

        public AuthSessionData TryRestoreSession()
        {
            if (!hasAttemptedRestore)
            {
                _ = TryRestoreSessionAsync();
            }

            return CurrentSession;
        }

        public AuthOperationResult Register(string username, string email, string password)
        {
            return RegisterAsync(username, email, password).GetAwaiter().GetResult();
        }

        public AuthOperationResult SignIn(string identifier, string password)
        {
            return SignInAsync(identifier, password).GetAwaiter().GetResult();
        }

        public void SignOut()
        {
            SignOutAsync().GetAwaiter().GetResult();
        }

        public Task<AuthSessionData> TryRestoreSessionAsync(CancellationToken cancellationToken = default)
        {
            if (restoreSessionTask != null && !restoreSessionTask.IsCompleted)
            {
                return restoreSessionTask;
            }

            restoreSessionTask = RestoreSessionInternalAsync(cancellationToken);
            return restoreSessionTask;
        }

        public async Task<AuthOperationResult> RegisterAsync(string username, string email, string password, CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return AuthOperationResult.Failed("Registration is managed on nekuzaky.com. Create your account on the website, then sign in here.");
        }

        public async Task<AuthOperationResult> SignInAsync(string identifier, string password, CancellationToken cancellationToken = default)
        {
            string safeEmail = string.IsNullOrWhiteSpace(identifier) ? string.Empty : identifier.Trim();
            string safePassword = string.IsNullOrWhiteSpace(password) ? string.Empty : password.Trim();

            if (string.IsNullOrWhiteSpace(safeEmail) || string.IsNullOrWhiteSpace(safePassword))
            {
                return AuthOperationResult.Failed("Enter email and password.");
            }

            if (!IsLikelyEmail(safeEmail))
            {
                return AuthOperationResult.Failed("Enter a valid email address.");
            }

            LoginRequest request = new LoginRequest
            {
                identifier = safeEmail,
                email = safeEmail,
                password = safePassword
            };

            ApiRequestResult<LoginResponseData> loginResult = await apiClient.PostAsync<LoginRequest, LoginResponseData>(
                ApiRoutes.AuthLogin,
                request,
                bearerToken: null,
                cancellationToken);

            if (!loginResult.Success)
            {
                if (loginResult.IsUnauthorized)
                {
                    return AuthOperationResult.Failed("Invalid credentials.");
                }

                return AuthOperationResult.Failed(string.IsNullOrWhiteSpace(loginResult.ErrorMessage)
                    ? "Unable to connect to server."
                    : loginResult.ErrorMessage);
            }

            LoginResponseData responseData = loginResult.Data;
            string token = responseData != null ? responseData.ResolveToken() : string.Empty;
            if (string.IsNullOrWhiteSpace(token))
            {
                token = ExtractTokenFromRawJson(loginResult.RawBody);
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                Debug.LogWarning($"[MINDRIFT][Auth] Login response had success=true but no recognized token field. Raw body: {loginResult.RawBody}");
                return AuthOperationResult.Failed("Login succeeded but token is missing.");
            }

            UserSummary user = responseData != null ? responseData.user : null;
            if (user == null)
            {
                ApiRequestResult<UserSummary> meResult = await apiClient.GetAsync<UserSummary>(ApiRoutes.AuthMe, token, cancellationToken);
                if (meResult.Success)
                {
                    user = meResult.Data;
                }
            }

            user ??= BuildFallbackUser(safeEmail);
            SetAuthenticatedSession(token, user, safeEmail);

            _ = MindriftOnlineService.Instance.PullRemoteSettingsAndApplyAsync(cancellationToken);
            return AuthOperationResult.Succeeded(CurrentSession, $"Welcome, {CurrentSession.DisplayName}.");
        }

        public Task SignOutAsync(CancellationToken cancellationToken = default)
        {
            TokenStorage.Clear();
            SetGuestSession();
            return Task.CompletedTask;
        }

        private async Task<AuthSessionData> RestoreSessionInternalAsync(CancellationToken cancellationToken)
        {
            hasAttemptedRestore = true;
            string token = TokenStorage.LoadToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                SetGuestSession();
                return CurrentSession;
            }

            ApiRequestResult<UserSummary> meResult = await apiClient.GetAsync<UserSummary>(
                ApiRoutes.AuthMe,
                token,
                cancellationToken);

            if (meResult.Success && meResult.Data != null)
            {
                SetAuthenticatedSession(token, meResult.Data, fallbackIdentifier: string.Empty);
                _ = MindriftOnlineService.Instance.PullRemoteSettingsAndApplyAsync(cancellationToken);
                return CurrentSession;
            }

            if (meResult.IsUnauthorized)
            {
                TokenStorage.Clear();
            }

            SetGuestSession();
            return CurrentSession;
        }

        private void SetAuthenticatedSession(string token, UserSummary user, string fallbackIdentifier)
        {
            user ??= new UserSummary();
            user.Sanitize();
            string userId = user.ResolveUserId();
            string username = user.ResolveDisplayName();

            if (!user.HasDisplayIdentity() && !string.IsNullOrWhiteSpace(fallbackIdentifier))
            {
                username = fallbackIdentifier.Trim();
            }

            string resolvedEmail = user.email ?? string.Empty;
            if (string.IsNullOrWhiteSpace(resolvedEmail) &&
                !string.IsNullOrWhiteSpace(fallbackIdentifier) &&
                fallbackIdentifier.Contains("@", StringComparison.Ordinal))
            {
                resolvedEmail = fallbackIdentifier.Trim();
            }

            AuthSessionData session = new AuthSessionData
            {
                userId = string.IsNullOrWhiteSpace(userId) ? username.ToLowerInvariant() : userId,
                username = username,
                email = resolvedEmail,
                authToken = string.IsNullOrWhiteSpace(token) ? string.Empty : token.Trim(),
                signedInAtUtc = DateTime.UtcNow.ToString("O"),
                provider = "nekuzaky_api",
                isGuest = false
            };
            session.Sanitize();

            currentUser = user;
            currentSession = session;
            TokenStorage.SaveToken(session.authToken);
            SessionChanged?.Invoke(currentSession);
        }

        private void SetGuestSession()
        {
            currentUser = null;
            currentSession = AuthSessionData.CreateGuest();
            SessionChanged?.Invoke(currentSession);
        }

        private static UserSummary BuildFallbackUser(string identifier)
        {
            string trimmed = string.IsNullOrWhiteSpace(identifier) ? "PLAYER" : identifier.Trim();
            return new UserSummary
            {
                id = string.Empty,
                user_id = string.Empty,
                username = trimmed,
                display_name = trimmed,
                email = trimmed.Contains("@", StringComparison.Ordinal) ? trimmed : string.Empty
            };
        }

        private static bool IsLikelyEmail(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string trimmed = value.Trim();
            int at = trimmed.IndexOf('@');
            if (at <= 0 || at >= trimmed.Length - 1)
            {
                return false;
            }

            int dot = trimmed.LastIndexOf('.');
            return dot > at + 1 && dot < trimmed.Length - 1;
        }

        private static string ExtractTokenFromRawJson(string rawBody)
        {
            if (string.IsNullOrWhiteSpace(rawBody))
            {
                return string.Empty;
            }

            const string pattern =
                "\"(?:token|access_token|accessToken|auth_token|authToken|bearer_token|bearerToken|jwt|jwt_token|id_token|session_token)\"\\s*:\\s*\"([^\"]+)\"";

            Match match = Regex.Match(rawBody, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success || match.Groups.Count < 2)
            {
                return string.Empty;
            }

            string tokenValue = match.Groups[1].Value;
            if (string.IsNullOrWhiteSpace(tokenValue))
            {
                return string.Empty;
            }

            tokenValue = tokenValue.Trim();
            if (tokenValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                tokenValue = tokenValue.Substring("Bearer ".Length).Trim();
            }

            return tokenValue;
        }
    }
}
