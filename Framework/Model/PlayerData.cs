namespace SaveAnywhere.Framework.Model
{
    public class PlayerData
    {
        public int Time { get; init; }
        public BuffData[] OtherBuffs { get; init; }

        public BuffData DrinkBuff { get; init; }

        public BuffData FoodBuff { get; init; }
        public PositionData[] Position { get; init; }

        public bool IsCharacterSwimming { get; init; }
    }
}