namespace Mindrift.Auth
{
    public static class AuthRuntime
    {
        private static IAuthService service;

        public static IAuthService Service => service ??= new LocalAuthService();

        public static void Override(IAuthService customService)
        {
            service = customService;
        }
    }
}
