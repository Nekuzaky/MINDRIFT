using System;

namespace Mindrift.Auth
{
    [Serializable]
    public sealed class AuthUserProfile
    {
        public string userId;
        public string username;
        public string email;
        public string createdAtUtc;

        public string DisplayName => string.IsNullOrWhiteSpace(username) ? email ?? "PLAYER" : username;

        public void Sanitize()
        {
            userId ??= string.Empty;
            username ??= string.Empty;
            email ??= string.Empty;
            createdAtUtc ??= string.Empty;
        }
    }
}
