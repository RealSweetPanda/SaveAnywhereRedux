namespace SaveAnywhere.Framework.Model {
    internal sealed record CharacterData(
        CharacterType Type,
        string Name,
        string Map,
        int X,
        int Y,
        int FacingDirection
    );
}