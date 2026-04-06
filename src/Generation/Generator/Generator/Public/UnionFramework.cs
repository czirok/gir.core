using Generator.Model;

namespace Generator.Generator.Public;

internal class UnionFramework : Generator<GirModel.Union>
{
    private readonly Publisher _publisher;

    public UnionFramework(Publisher publisher)
    {
        _publisher = publisher;
    }

    public void Generate(GirModel.Union union)
    {
        if (union.TypeFunction is null)
            return;

        var source = Renderer.Public.UnionFramework.Render(union);
        var codeUnit = new CodeUnit(
            Project: Namespace.GetCanonicalName(union.Namespace),
            Name: $"{union.Name}.Framework",
            Source: source,
            IsInternal: false
        );

        _publisher.Publish(codeUnit);
    }
}
