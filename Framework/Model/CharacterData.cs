namespace SaveAnywhere.Framework.Model {
    public sealed record CharacterData(
        CharacterType Type,
        string Name,
        string Map,
        int X,
        int Y,
        int FacingDirection
    );
}