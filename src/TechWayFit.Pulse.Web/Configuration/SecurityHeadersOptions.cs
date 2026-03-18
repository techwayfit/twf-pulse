namespace TechWayFit.Pulse.Web.Configuration;

public class SecurityHeadersOptions
{
    public const string SectionName = "SecurityHeaders";

    public CspOptions ContentSecurityPolicy { get; set; } = new();
}

public class CspOptions
{
    public string[] DefaultSrc { get; set; } = ["'self'"];
    public string[] BaseUri { get; set; } = ["'self'"];
    public string[] FrameAncestors { get; set; } = ["'self'"];
    public string[] FormAction { get; set; } = ["'self'"];
    public string[] ImgSrc { get; set; } = ["'self'", "data:", "blob:", "https:"];
    public string[] FontSrc { get; set; } = ["'self'", "data:", "https://fonts.gstatic.com"];
    public string[] StyleSrc { get; set; } = ["'self'", "'unsafe-inline'", "https://fonts.googleapis.com"];
    public string[] ScriptSrc { get; set; } = ["'self'", "'unsafe-inline'", "'unsafe-eval'"];
    public string[] ConnectSrc { get; set; } = ["'self'", "https:", "wss:", "ws:"];

    public string Build()
    {
        return string.Join(" ",
        [
            Directive("default-src", DefaultSrc),
            Directive("base-uri", BaseUri),
            Directive("frame-ancestors", FrameAncestors),
            Directive("form-action", FormAction),
            Directive("img-src", ImgSrc),
            Directive("font-src", FontSrc),
            Directive("style-src", StyleSrc),
            Directive("script-src", ScriptSrc),
            Directive("connect-src", ConnectSrc),
        ]);
    }

    private static string Directive(string name, string[] sources) =>
        $"{name} {string.Join(" ", sources)};";
}
