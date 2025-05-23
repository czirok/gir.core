using System;
using System.Linq;
using Generator.Model;

namespace Generator.Renderer.Public;

internal static class Enumeration
{
    public static string Render(GirModel.Enumeration enumeration)
    {
        return $@"
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

#nullable enable

namespace {Namespace.GetPublicName(enumeration.Namespace)};

// AUTOGENERATED FILE - DO NOT MODIFY

{PlatformSupportAttribute.Render(enumeration as GirModel.PlatformDependent)}
public enum {enumeration.Name} : int
{{
    {enumeration
        .Members
        .Select(MemberRenderer.Render)
        .Join(Environment.NewLine)}
}}";
    }
}
