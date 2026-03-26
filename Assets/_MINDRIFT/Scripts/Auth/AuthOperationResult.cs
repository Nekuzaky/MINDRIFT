namespace Mindrift.Auth
{
    public sealed class AuthOperationResult
    {
        public bool success;
        public string message;
        public AuthSessionData session;

        public static AuthOperationResult Succeeded(AuthSessionData session, string message)
        {
            return new AuthOperationResult
            {
                success = true,
                message = message ?? string.Empty,
                session = session
            };
        }

        public static AuthOperationResult Failed(string message)
        {
            return new AuthOperationResult
            {
                success = false,
                message = message ?? "Authentication failed.",
                session = null
            };
        }
    }
}
