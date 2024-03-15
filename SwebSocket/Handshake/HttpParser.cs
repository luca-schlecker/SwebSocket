namespace SwebSocket;

internal static class HttpParser
{
    public static IHttpMachine.Model.HttpRequestResponse Parse(byte[] bytes)
    {
        using var del = new HttpMachine.HttpParserDelegate();
        using var parser = new HttpMachine.HttpCombinedParser(del);

        if (bytes.Length != parser.Execute(bytes))
            throw new Exception("Invalid HTTP Request");

        parser.Execute(default(ArraySegment<byte>));

        return del.HttpRequestResponse;
    }
}