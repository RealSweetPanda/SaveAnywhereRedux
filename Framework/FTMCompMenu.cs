using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace SaveAnywhere.Framework
{
    /// <summary>The menu which lets the player choose their birthday.</summary>
    public class FTMCompMenu : IClickableMenu
    {
        public override bool shouldDrawCloseButton() => false;
        public override void update(GameTime time)
        {
            exitThisMenu(false);
        }
    }
}