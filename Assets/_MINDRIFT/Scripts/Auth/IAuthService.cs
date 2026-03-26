using System;

namespace Mindrift.Auth
{
    public interface IAuthService
    {
        AuthSessionData CurrentSession { get; }
        event Action<AuthSessionData> SessionChanged;

        AuthSessionData TryRestoreSession();
        AuthOperationResult Register(string username, string email, string password);
        AuthOperationResult SignIn(string identifier, string password);
        void SignOut();
    }
}
