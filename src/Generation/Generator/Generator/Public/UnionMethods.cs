using Generator.Model;

namespace Generator.Generator.Public;

internal class UnionMethods : Generator<GirModel.Union>
{
    private readonly Publisher _publisher;

    public UnionMethods(Publisher publisher)
    {
        _publisher = publisher;
    }

    public void Generate(GirModel.Union union)
    {
        if (union.TypeFunction is null)
            return;

        var source = Renderer.Public.UnionMethods.Render(union);
        var codeUnit = new CodeUnit(
            Project: Namespace.GetCanonicalName(union.Namespace),
            Name: $"{union.Name}.Methods",
            Source: source,
            IsInternal: false
        );

        _publisher.Publish(codeUnit);
    }
}
