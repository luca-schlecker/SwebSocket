namespace SwebSocket;

public class WebSocketConnectionOptions
{
    public SSLConnectionOptions SSL { get; set; }
}

public class SSLConnectionOptions
{
    public bool UseSSL { get; set; }
    public string ValidatedAuthority { get; set; }
}