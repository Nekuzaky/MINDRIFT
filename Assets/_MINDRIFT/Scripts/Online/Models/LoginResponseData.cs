using System;

namespace Mindrift.Online.Models
{
    [Serializable]
    public sealed class LoginResponseData
    {
        public string token;
        public string access_token;
        public string accessToken;
        public string auth_token;
        public string authToken;
        public string bearer_token;
        public string bearerToken;
        public string jwt;
        public string jwt_token;
        public string id_token;
        public string session_token;
        public UserSummary user;

        public string ResolveToken()
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                return NormalizeToken(token);
            }

            if (!string.IsNullOrWhiteSpace(access_token))
            {
                return NormalizeToken(access_token);
            }

            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                return NormalizeToken(accessToken);
            }

            if (!string.IsNullOrWhiteSpace(auth_token))
            {
                return NormalizeToken(auth_token);
            }

            if (!string.IsNullOrWhiteSpace(authToken))
            {
                return NormalizeToken(authToken);
            }

            if (!string.IsNullOrWhiteSpace(bearer_token))
            {
                return NormalizeToken(bearer_token);
            }

            if (!string.IsNullOrWhiteSpace(bearerToken))
            {
                return NormalizeToken(bearerToken);
            }

            if (!string.IsNullOrWhiteSpace(jwt))
            {
                return NormalizeToken(jwt);
            }

            if (!string.IsNullOrWhiteSpace(jwt_token))
            {
                return NormalizeToken(jwt_token);
            }

            if (!string.IsNullOrWhiteSpace(id_token))
            {
                return NormalizeToken(id_token);
            }

            if (!string.IsNullOrWhiteSpace(session_token))
            {
                return NormalizeToken(session_token);
            }

            return string.Empty;
        }

        private static string NormalizeToken(string rawToken)
        {
            if (string.IsNullOrWhiteSpace(rawToken))
            {
                return string.Empty;
            }

            string tokenValue = rawToken.Trim();
            if (tokenValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                tokenValue = tokenValue.Substring("Bearer ".Length).Trim();
            }

            return tokenValue;
        }
    }
}
