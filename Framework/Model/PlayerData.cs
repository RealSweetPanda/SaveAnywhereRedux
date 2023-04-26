namespace SaveAnywhere.Framework.Model {
    public class PlayerData {
        public int Time { get; init; }
        public BuffData[] otherBuffs { get; init; }
        
        public BuffData drinkBuff { get; init; }
        
        public BuffData foodBuff { get; init; }
        public PositionData Position { get; init; }

        public bool IsCharacterSwimming { get; init; }
    }
}