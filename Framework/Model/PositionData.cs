namespace SaveAnywhere.Framework.Model
{
    public sealed record PositionData(
        string Name,
        string Map,
        int X,
        int Y,
        int FacingDirection
    );
}