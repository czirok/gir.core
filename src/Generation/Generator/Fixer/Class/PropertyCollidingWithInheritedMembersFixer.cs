using Generator.Model;

namespace Generator.Fixer.Class;

internal class PropertyCollidingWithInheritedMembersFixer : Fixer<GirModel.Class>
{
    public void Fixup(GirModel.Class @class)
    {
        foreach (var property in @class.Properties)
            RenameIfColliding(@class, property);

        foreach (var @interface in @class.Implements)
            foreach (var interfaceProperty in @interface.Properties)
                RenameIfColliding(@class, interfaceProperty);
    }

    private static void RenameIfColliding(GirModel.Class @class, GirModel.Property property)
    {
        var name = Property.GetName(property);

        // Generated/public classes expose a Handle member via their base runtime types.
        // A GIR property named "handle" would collide with that member and break codegen.
        if (name != "Handle")
            return;

        Property.SetName(property, $"{name}_");
        Log.Information(
            $"Property {property.Name} collides with an inherited member on {@class.Namespace.Name}.{@class.Name}. Renaming to {name}_.");
    }
}
