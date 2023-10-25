using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace SaveAnywhere.Framework
{
    public class FTMCompMenu : IClickableMenu
    {
        public override bool shouldDrawCloseButton() => false;

        public override void update(GameTime time)
        {
            exitThisMenu(false);
        }
    }
}