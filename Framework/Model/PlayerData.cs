namespace SaveAnywhere.Framework.Model {
    public class PlayerData {
        public int Time { get; set; }

        public CharacterData[] Characters { get; set; }

        public bool IsCharacterSwimming { get; set; }
    }
}