using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Sf = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Sk = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using St = Microsoft.CodeAnalysis.SyntaxToken;

internal static class EscapeString
{
    // Known numeric prefixes that have a well-established semantic name.
    // If an identifier starts with one of these strings, the numeric prefix
    // is replaced with the mapped value instead of the generic digit-word fallback.
    private static readonly Dictionary<string, string> KnownNumericPrefixes = new()
    {
        { "80211", "IEEE80211" }
    };

    public static string EscapeIdentifier(this string _text)
    {
        var escaped = FixFirstCharIfNumber(_text);
        escaped = FixIfIdentifierIsKeyword(escaped);

        return escaped;
    }

    private static string FixIfIdentifierIsKeyword(string identifier)
    {
        St token = Sf.ParseToken(identifier);
        if (!token.IsKind(Sk.IdentifierToken))
            identifier = "@" + identifier;

        return identifier;
    }

    private static string FixFirstCharIfNumber(string identifier)
    {
        var firstChar = identifier[0];

        if (char.IsNumber(firstChar))
        {
            foreach (var (numericPrefix, semanticPrefix) in KnownNumericPrefixes)
            {
                if (identifier.StartsWith(numericPrefix, StringComparison.Ordinal))
                    return semanticPrefix + identifier[numericPrefix.Length..];
            }

            // Capitalise Second Char
            if (identifier.Length > 1)
                identifier = CapitaliseSecondChar(identifier);

            var number = (int)char.GetNumericValue(firstChar);
            return number switch
            {
                0 => ReplaceFirstChar("Zero", identifier),
                1 => ReplaceFirstChar("One", identifier),
                2 => ReplaceFirstChar("Two", identifier),
                3 => ReplaceFirstChar("Three", identifier),
                4 => ReplaceFirstChar("Four", identifier),
                5 => ReplaceFirstChar("Five", identifier),
                6 => ReplaceFirstChar("Six", identifier),
                7 => ReplaceFirstChar("Seven", identifier),
                8 => ReplaceFirstChar("Eight", identifier),
                9 => ReplaceFirstChar("Nine", identifier),
                _ => throw new Exception("Can't fix identifier " + identifier)
            };
        }

        return identifier;
    }

    private static string ReplaceFirstChar(string prefix, string str)
        => prefix + str[1..];

    private static string CapitaliseSecondChar(string identifier)
        => $"{identifier[0]}{char.ToUpper(identifier[1])}{identifier?[2..]}";
}
