public enum ClientToServerSignifier
{
    SignUp = 1, //"1,name,password"
    SignIn = 2, //"2,name,password"
}
public enum ServerToClientSignifier
{
    SignUpSucceeded = 1, //"1"
    SignUpFailed = 2, //"2,reason"
    SignInSucceeded = 3, //"3"
    SignInFailed = 4, //"4,reason"
}

public static class NetworkProtocol
{
    public const char Separator = ',';

    public static bool IsValidField(string _s)
    {
        return !string.IsNullOrWhiteSpace(_s)
            && _s.IndexOfAny(new[] { Separator, '\n', '\r' }) < 0;
    }
}