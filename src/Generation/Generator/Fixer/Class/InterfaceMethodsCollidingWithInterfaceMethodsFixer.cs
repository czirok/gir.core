using System.Linq;
using Generator.Model;

namespace Generator.Fixer.Class;

internal class InterfaceMethodsCollidingWithInterfaceMethodsFixer : Fixer<GirModel.Class>
{
    public void Fixup(GirModel.Class @class)
    {
        var interfaceMethods = @class.Implements
            .SelectMany(Model.Interface.AllMethods)
            .ToArray();

        foreach (var method in interfaceMethods)
        {
            var duplicateMethods = Model.Class.DuplicateMethods(@class, method)
                .Where(x => x.Parent is GirModel.Interface)
                .ToArray();

            if (duplicateMethods.Length == 0)
                continue;

            Method.SetImplementExplicitly(method);
        }
    }
}
