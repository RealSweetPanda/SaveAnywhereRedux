// Decompiled with JetBrains decompiler
// Type: Omegasis.SaveAnywhere.Framework.NewShippingMenu
// Assembly: SaveAnywhere, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: CA1E3B07-AC71-4821-90DC-80822753C1D9
// Assembly location: C:\Users\keren\Desktop\SaveAnywhere1.5\SaveAnywhere.dll

using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;

namespace Omegasis.SaveAnywhere.Framework
{
  internal class NewShippingMenu : ShippingMenu
  {
    private readonly IReflectedField<bool> SavedYet;

    public NewShippingMenu(NetCollection<Item> items, IReflectionHelper reflection)
      : base((IList<Item>) items)
    {
      this.SavedYet = reflection.GetField<bool>((object) this, "savedYet", true);
      NetCollection<Item> shippingBin = Game1.getFarm().getShippingBin(Game1.player);
      if (Game1.player.useSeparateWallets || !Game1.player.useSeparateWallets && Game1.player.IsMainPlayer)
      {
        int num = 0;
        foreach (Item obj in shippingBin)
        {
          if (obj is Object)
            num += (obj as Object).sellToStorePrice(-1L) * obj.Stack;
        }
        Game1.player.Money += num;
        Game1.getFarm().getShippingBin(Game1.player).Clear();
      }
      if (!Game1.player.useSeparateWallets || !Game1.player.IsMainPlayer)
        return;
      foreach (Farmer allFarmhand in Game1.getAllFarmhands())
      {
        if (!allFarmhand.isActive() && !allFarmhand.isUnclaimedFarmhand)
        {
          int num = 0;
          foreach (Item obj in Game1.getFarm().getShippingBin(allFarmhand))
          {
            if (obj is Object)
              num += (obj as Object).sellToStorePrice(allFarmhand.UniqueMultiplayerID) * obj.Stack;
          }
          Game1.player.team.AddIndividualMoney(allFarmhand, num);
          Game1.getFarm().getShippingBin(allFarmhand).Clear();
        }
      }
    }

    public virtual void receiveLeftClick(int x, int y, bool playSound = true)
    {
      if (((ClickableComponent) this.okButton).containsPoint(x, y))
        ((IClickableMenu) this).exitThisMenu(true);
      base.receiveLeftClick(x, y, playSound);
    }

    public virtual void update(GameTime time)
    {
      this.SavedYet.SetValue(true);
      base.update(time);
    }
  }
}
