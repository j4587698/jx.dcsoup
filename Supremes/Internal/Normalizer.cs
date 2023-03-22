namespace Supremes.Internal;

/// <summary>
/// Util methods for normalizing strings. Jsoup internal use only, please don't depend on this API.
/// </summary>
public static class Normalizer {

    /** Drops the input string to lower case. */
    public static string LowerCase(string input) {
        return input != null ? input.ToLower() : "";
    }

    /** Lower-cases and trims the input string. */
    public static string Normalize(string input) {
        return LowerCase(input).Trim();
    }

    /** If a string literal, just lower case the string; otherwise lower-case and trim. */
    public static string Normalize(string input, bool isStringLiteral) {
        return isStringLiteral ? LowerCase(input) : Normalize(input);
    }
}