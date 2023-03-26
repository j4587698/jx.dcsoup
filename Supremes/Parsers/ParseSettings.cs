using Supremes.Nodes;

namespace Supremes.Parsers;

/// <summary>
/// Controls parser case settings, to optionally preserve tag and/or attribute name case.
/// </summary>
public class ParseSettings
{
    /// <summary>
    /// HTML default settings: both tag and attribute names are lower-cased during parsing.
    /// </summary>
    public static readonly ParseSettings HtmlDefault = new ParseSettings(false, false);

    /// <summary>
    /// Preserve both tag and attribute case.
    /// </summary>
    public static readonly ParseSettings PreserveCase = new ParseSettings(true, true);

    private readonly bool _preserveTagCase;
    private readonly bool _preserveAttributeCase;

    /// <summary>
    /// Returns true if preserving tag name case.
    /// </summary>
    public bool PreserveTagCase => _preserveTagCase;

    /// <summary>
    /// Returns true if preserving attribute case.
    /// </summary>
    public bool PreserveAttributeCase => _preserveAttributeCase;

    /// <summary>
    /// Define parse settings.
    /// </summary>
    /// <param name="tag">preserve tag case?</param>
    /// <param name="attribute">preserve attribute name case?</param>
    public ParseSettings(bool tag, bool attribute)
    {
        _preserveTagCase = tag;
        _preserveAttributeCase = attribute;
    }

    internal ParseSettings(ParseSettings copy)
    {
        _preserveTagCase = copy._preserveTagCase;
        _preserveAttributeCase = copy._preserveAttributeCase;
    }

    /// <summary>
    /// Normalizes a tag name according to the case preservation setting.
    /// </summary>
    public string NormalizeTag(string name)
    {
        name = name.Trim();
        if (!_preserveTagCase)
            name = name.ToLower();
        return name;
    }

    /// <summary>
    /// Normalizes an attribute according to the case preservation setting.
    /// </summary>
    public string NormalizeAttribute(string name)
    {
        name = name.Trim();
        if (!_preserveAttributeCase)
            name = name.ToLower();
        return name;
    }

    /// <summary>
    /// Normalizes an attribute according to the case preservation setting.
    /// </summary>
    /// <param name="attributes"></param>
    /// <returns></returns>
    public Attributes NormalizeAttributes(Attributes attributes)
    {
        if (attributes != null && !_preserveAttributeCase)
        {
            attributes.Normalize();
        }

        return attributes;
    }

    /// <summary>
    /// Returns the normal name that a Tag will have (trimmed and lower-cased)
    /// </summary>
    public static string NormalName(string name)
    {
        return name.Trim().ToLower();
    }
}