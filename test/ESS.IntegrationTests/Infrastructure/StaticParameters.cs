namespace ESS.IntegrationTests.Infrastructure;
public static class StaticParameters
{
    public static class Routes
    {
        public static class Auth
        {
            public const string Register = "auth/register";
            public const string Login = "auth/login";
        }

        public static class AppSettings
        {
            public const string Settings = "settings";
        }
    }

    public static class MockUsers
    {
        public static class ValidEmployee
        {
            public const string NationalCode = "2220008533";
            public const string Password = "Rez@0911";
            public const string PhoneNumber = "09351900775";
        }

        public static class ValidAdmin
        {
            public const string NationalCode = "4444444444";
            public const string Password = "9110!Reza";
        }

        // For Concurrency Control
        public static class AnotherValidEmployee
        {
            public const string NationalCode = "5010027501";
            public const string Password = "123!@#qweQWE";
            public const string PhoneNumber = "09119136051";
        }

        public static class InValidEmployee
        {
            public const string NationalCode = "2130000000";
            public const string Password = "123456";
            public const string PhoneNumber = "09000000000";
        }

    }
}
