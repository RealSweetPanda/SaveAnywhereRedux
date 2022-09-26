using Microsoft.Xna.Framework;

namespace SaveAnywhere.Framework.Model {
    public class CharacterData {
        public CharacterData() { }

        public CharacterData(
            CharacterType type,
            string name,
            string map,
            int x,
            int y,
            int facingDirection) {
            Type = type;
            Name = name;
            Map = map;
            X = x;
            Y = y;
            FacingDirection = facingDirection;
        }

        public CharacterData(
            CharacterType type,
            string name,
            string map,
            Point tile,
            int facingDirection)
            : this(type, name, map, tile.X, tile.Y, facingDirection) { }

        public CharacterType Type { get; set; }

        public string Name { get; set; }

        public string Map { get; set; }

        public int X { get; set; }

        public int Y { get; set; }

        public int FacingDirection { get; set; }
    }
}