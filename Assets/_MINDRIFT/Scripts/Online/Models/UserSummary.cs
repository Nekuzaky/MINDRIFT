using System;

namespace Mindrift.Online.Models
{
    [Serializable]
    public sealed class UserSummary
    {
        public string id;
        public string userId;
        public string user_id;
        public string uid;
        public string username;
        public string user_name;
        public string login;
        public string name;
        public string nickname;
        public string pseudo;
        public string display_name;
        public string displayName;
        public string email;

        public string ResolveUserId()
        {
            if (!string.IsNullOrWhiteSpace(user_id))
            {
                return user_id.Trim();
            }

            if (!string.IsNullOrWhiteSpace(userId))
            {
                return userId.Trim();
            }

            if (!string.IsNullOrWhiteSpace(uid))
            {
                return uid.Trim();
            }

            if (!string.IsNullOrWhiteSpace(id))
            {
                return id.Trim();
            }

            return string.Empty;
        }

        public string ResolveDisplayName()
        {
            if (!string.IsNullOrWhiteSpace(display_name))
            {
                return display_name.Trim();
            }

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(username))
            {
                return username.Trim();
            }

            if (!string.IsNullOrWhiteSpace(user_name))
            {
                return user_name.Trim();
            }

            if (!string.IsNullOrWhiteSpace(login))
            {
                return login.Trim();
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                return name.Trim();
            }

            if (!string.IsNullOrWhiteSpace(nickname))
            {
                return nickname.Trim();
            }

            if (!string.IsNullOrWhiteSpace(pseudo))
            {
                return pseudo.Trim();
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                return email.Trim();
            }

            return "PLAYER";
        }

        public bool HasDisplayIdentity()
        {
            return !string.IsNullOrWhiteSpace(display_name) ||
                   !string.IsNullOrWhiteSpace(displayName) ||
                   !string.IsNullOrWhiteSpace(username) ||
                   !string.IsNullOrWhiteSpace(user_name) ||
                   !string.IsNullOrWhiteSpace(login) ||
                   !string.IsNullOrWhiteSpace(name) ||
                   !string.IsNullOrWhiteSpace(nickname) ||
                   !string.IsNullOrWhiteSpace(pseudo) ||
                   !string.IsNullOrWhiteSpace(email);
        }

        public void Sanitize()
        {
            id ??= string.Empty;
            userId ??= string.Empty;
            user_id ??= string.Empty;
            uid ??= string.Empty;
            username ??= string.Empty;
            user_name ??= string.Empty;
            login ??= string.Empty;
            name ??= string.Empty;
            nickname ??= string.Empty;
            pseudo ??= string.Empty;
            display_name ??= string.Empty;
            displayName ??= string.Empty;
            email ??= string.Empty;
        }
    }
}
