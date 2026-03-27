using System;

namespace Mindrift.Online.Models
{
    [Serializable]
    public sealed class LoginRequest
    {
        public string identifier;
        public string email;
        public string password;
    }
}
