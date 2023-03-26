using System;

namespace Supremes;

/// <summary>
///  The Connection interface is a convenient HTTP client and session object to fetch content from the web, and parse them
///  into Documents.
///  <p>To start a new session, use either {@link org.jsoup.Jsoup#newSession()} or {@link org.jsoup.Jsoup#connect(String)}.
///  Connections contain {@link Connection.Request} and {@link Connection.Response} objects (once executed). Configuration
///  settings (URL, timeout, useragent, etc) set on a session will be applied by default to each subsequent request.</p>
///  <p>To start a new request from the session, use {@link #newRequest()}.</p>
///  <p>Cookies are stored in memory for the duration of the session. For that reason, do not use one single session for all
///  requests in a long-lived application, or you are likely to run out of memory, unless care is taken to clean up the
///  cookie store. The cookie store for the session is available via {@link #cookieStore()}. You may provide your own
///  implementation via {@link #cookieStore(java.net.CookieStore)} before making requests.</p>
///  <p>Request configuration can be made using either the shortcut methods in Connection (e.g. {@link #userAgent(String)}),
///  or by methods in the Connection.Request object directly. All request configuration must be made before the request is
///  executed. When used as an ongoing session, initialize all defaults prior to making multi-threaded {@link
/// #newRequest()}s.</p>
///  <p>Note that the term "Connection" used here does not mean that a long-lived connection is held against a server for
///  the lifetime of the Connection object. A socket connection is only made at the point of request execution ({@link
/// #execute()}, {@link #get()}, or {@link #post()}), and the server's response consumed.</p>
///  <p>For multi-threaded implementations, it is important to use a {@link #newRequest()} for each request. The session may
///  be shared across threads but a given request, not.</p>
/// </summary>
public interface IConnection
{
    /// <summary>
    /// GET and POST http methods.
    /// </summary>
    public enum Method {
        Get, 
        Post, 
        Put, 
        Delete, 
        Patch, 
        Head, 
        Options, 
        Trace
    }
    
    /// <summary>
    /// Creates a new request, using this Connection as the session-state and to initialize the connection settings (which may then be independently on the returned Connection.Request object).
    /// </summary>
    /// <returns>a new Connection object, with a shared Cookie Store and initialized settings from this Connection and Request</returns>
    IConnection NewRequest();

    /// <summary>
    ///  Set the request URL to fetch. The protocol must be HTTP or HTTPS.
    /// </summary>
    /// <param name="url">URL to connect to</param>
    /// <returns>this Connection, for chaining</returns>
    IConnection Url(Uri url);

    /// <summary>
    /// Set the request URL to fetch. The protocol must be HTTP or HTTPS.
    /// </summary>
    /// <param name="url">URL to connect to</param>
    /// <returns>this Connection, for chaining</returns>
    IConnection Url(string url);
}