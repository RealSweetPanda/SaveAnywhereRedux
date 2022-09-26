namespace SaveAnywhere.Framework.Model {
    public class PlayerData {
        public int Time { get; init; }

        public CharacterData[] Characters { get; init; }

        public bool IsCharacterSwimming { get; init; }
    }
}