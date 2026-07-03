namespace SaveAnywhere.Framework.Model
{
    public sealed record DebrisData(
        string Map,
        int X,
        int Y,
        string QualifiedItemId,
        int Stack,
        int Quality
    );
}
