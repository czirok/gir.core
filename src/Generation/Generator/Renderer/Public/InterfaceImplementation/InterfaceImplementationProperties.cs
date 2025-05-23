using System;
using System.Linq;
using System.Text;
using Generator.Model;

namespace Generator.Renderer.Public;

public static class InterfaceImplementationProperties
{
    public static string Render(GirModel.Interface @interface)
    {
        return $@"
using System;
using GObject;
using System.Runtime.InteropServices;

#nullable enable

namespace {Namespace.GetPublicName(@interface.Namespace)};

// AUTOGENERATED FILE - DO NOT MODIFY

public partial class {Interface.GetImplementationName(@interface)}
{{
    {@interface.Properties
        .Where(Property.IsEnabled)
        .Select(prop => Render(@interface, prop))
        .Join(Environment.NewLine)}
}}";
    }

    private static string Render(GirModel.ComplexType complexType, GirModel.Property property)
    {
        try
        {
            Property.ThrowIfNotSupported(complexType, property);

            var builder = new StringBuilder();
            builder.AppendLine(RenderAccessor(complexType, property));

            return builder.ToString();
        }
        catch (Exception ex)
        {
            var message = $"Did not generate property '{complexType.Name}.{property.Name}': {ex.Message}";

            if (ex is NotImplementedException)
                Log.Debug(message);
            else
                Log.Warning(message);

            return string.Empty;
        }
    }

    private static string RenderAccessor(GirModel.ComplexType complexType, GirModel.Property property)
    {
        if (property is { Readable: false, Writeable: false })
            return string.Empty;

        var builder = new StringBuilder();
        builder.AppendLine($"public {Property.GetNullableTypeName(property)} {Property.GetName(property)}");
        builder.AppendLine("{");

        if (property.Readable)
            builder.AppendLine($"    get => {GetGetter(complexType, property)};");

        if (property is { Writeable: true, ConstructOnly: false })
            builder.AppendLine($"    set => {GetSetter(complexType, property)};");

        builder.AppendLine("}");

        return builder.ToString();
    }

    private static string GetGetter(GirModel.ComplexType complexType, GirModel.Property property)
    {
        return Property.SupportsAccessorGetMethod(property, out var getter)
            ? $"{Namespace.GetInternalName(complexType.Namespace)}.{complexType.Name}.{Method.GetInternalName(getter)}(Handle.DangerousGetHandle())"
            : $"{ComplexType.GetFullyQualified(complexType)}.{Property.GetDescriptorName(property)}.Get(this)";
    }

    private static string GetSetter(GirModel.ComplexType complexType, GirModel.Property property)
    {
        return Property.SupportsAccessorSetMethod(property, out var setter)
            ? $"{Namespace.GetInternalName(complexType.Namespace)}.{complexType.Name}.{Method.GetInternalName(setter)}(Handle.DangerousGetHandle(), value)"
            : $"{ComplexType.GetFullyQualified(complexType)}.{Property.GetDescriptorName(property)}.Set(this, value)";
    }
}
