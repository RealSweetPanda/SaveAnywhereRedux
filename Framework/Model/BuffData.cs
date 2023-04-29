namespace SaveAnywhere.Framework.Model
{
    public sealed record BuffData(
        string DisplaySource,
        string Source,
        int MillisecondsDuration,
        int[] Attributes
    );
}