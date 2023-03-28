using System;
using System.Text;

namespace Supremes.Helper;

/// <summary>
/// Implementation of {@link Connection}.
/// </summary>
public class HttpConnection : IConnection
{
    public static readonly string DEFAULT_UA =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.130 Safari/537.36";
    private static readonly string USER_AGENT = "User-Agent";
    public static readonly string CONTENT_TYPE = "Content-Type";
    public static readonly string MULTIPART_FORM_DATA = "multipart/form-data";
    public static readonly string FORM_URL_ENCODED = "application/x-www-form-urlencoded";
    private static readonly int HTTP_TEMP_REDIR = 307; // http/1.1 temporary redirect, not in Java's set.
    private static readonly string DefaultUploadType = "application/octet-stream";
    private static readonly Encoding UTF_8 = Encoding.UTF8; // Don't use StandardCharsets, not in Android API 10.
    private static readonly Encoding ISO_8859_1 = Encoding.GetEncoding("ISO-8859-1");

    /// <summary>
    /// Create a new Connection, with the request URL specified.
    /// </summary>
    /// <param name="url">the URL to fetch from</param>
    /// <returns>a new Connection object</returns>
    public static IConnection Connect(string url)
    {
        IConnection conn = new HttpConnection();
        conn.Url(url);
        return conn;
    }

    /// <summary>
    /// Create a new Connection, with the request URL specified.
    /// </summary>
    /// <param name="uri">the URL to fetch from</param>
    /// <returns>a new Connection object</returns>
    public static IConnection Connect(Uri uri)
    {
        IConnection conn = new HttpConnection();
        conn.Url(uri);
        return conn;
    }
    
    public IConnection NewRequest()
    {
        throw new NotImplementedException();
    }

    public IConnection Url(Uri url)
    {
        throw new NotImplementedException();
    }

    public IConnection Url(string url)
    {
        throw new NotImplementedException();
    }
}