using System;

namespace Mindrift.Auth
{
    [Serializable]
    public sealed class AuthSessionData
    {
        public string userId;
        public string username;
        public string email;
        public string authToken;
        public string signedInAtUtc;
        public string provider;
        public bool isGuest;

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(username))
                {
                    return username.Trim();
                }

                if (!string.IsNullOrWhiteSpace(email))
                {
                    return email.Trim();
                }

                return isGuest ? "GUEST" : "PLAYER";
            }
        }

        public string StatsProfileKey
        {
            get
            {
                if (isGuest || string.IsNullOrWhiteSpace(userId))
                {
                    return "guest";
                }

                return userId.Trim();
            }
        }

        public void Sanitize()
        {
            userId ??= string.Empty;
            username ??= string.Empty;
            email ??= string.Empty;
            authToken ??= string.Empty;
            signedInAtUtc ??= string.Empty;
            provider = string.IsNullOrWhiteSpace(provider) ? "local" : provider.Trim();
        }

        public static AuthSessionData CreateGuest()
        {
            AuthSessionData session = new AuthSessionData
            {
                userId = "guest",
                username = "GUEST",
                email = string.Empty,
                authToken = string.Empty,
                signedInAtUtc = string.Empty,
                provider = "local",
                isGuest = true
            };

            session.Sanitize();
            return session;
        }
    }
}
