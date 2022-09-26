// Decompiled with JetBrains decompiler
// Type: Omegasis.SaveAnywhere.Framework.NewSaveGameMenu
// Assembly: SaveAnywhere, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: CA1E3B07-AC71-4821-90DC-80822753C1D9
// Assembly location: C:\Users\keren\Desktop\SaveAnywhere1.5\SaveAnywhere.dll

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Text;
using xTile.Dimensions;

namespace Omegasis.SaveAnywhere.Framework
{
  // internal class NewSaveGameMenu : SaveGameMenu
  // {
  //   private int completePause = -1;
  //   private int margin = 500;
  //   private readonly StringBuilder _stringBuilder = new StringBuilder();
  //   private float _ellipsisDelay = 0.5f;
  //   private IEnumerator<int> loader;
  //   public bool quit;
  //   public bool hasDrawn;
  //   private readonly SparklingText saveText;
  //   private int _ellipsisCount;
  //
  //   public event EventHandler SaveComplete;
  //
  //   public NewSaveGameMenu()
  //   {
  //     SpriteFont dialogueFont = Game1.dialogueFont;
  //     string str = Game1.content.LoadString("Strings\\StringsFromCSFiles:SaveGameMenu.cs.11378");
  //     Color limeGreen = Color.LimeGreen;
  //     Color black = Color.Black;
  //     int num1 = (int) ((double) ((Color) ref black).R * 0.001);
  //     black = Color.Black;
  //     int num2 = (int) ((double) ((Color) ref black).G * 0.001);
  //     black = Color.Black;
  //     int num3 = (int) ((double) ((Color) ref black).B * 0.001);
  //     Color color = new Color(num1, num2, num3, (int) byte.MaxValue);
  //     this.saveText = new SparklingText(dialogueFont, str, limeGreen, color, false, 0.1, 1500, 32, 500, 1f);
  //   }
  //
  //   public void complete()
  //   {
  //     Game1.playSound("money");
  //     this.completePause = 1500;
  //     this.loader = (IEnumerator<int>) null;
  //     Game1.game1.IsSaving = false;
  //     this.SaveComplete((object) this, EventArgs.Empty);
  //   }
  //
  //   public virtual void update(GameTime time)
  //   {
  //     if (this.quit)
  //       return;
  //     if (Game1.client != null && Game1.client.timedOut)
  //     {
  //       this.quit = true;
  //       if (!((object) Game1.activeClickableMenu).Equals((object) this))
  //         return;
  //       Game1.player.checkForLevelTenStatus();
  //       Game1.exitActiveMenu();
  //     }
  //     else
  //     {
  //       if (this.loader != null)
  //       {
  //         this.loader.MoveNext();
  //         if (this.loader.Current >= 100)
  //         {
  //           this.margin -= time.ElapsedGameTime.Milliseconds;
  //           if (this.margin <= 0)
  //             this.complete();
  //         }
  //         this._ellipsisDelay -= (float) time.ElapsedGameTime.TotalSeconds;
  //         if ((double) this._ellipsisDelay <= 0.0)
  //         {
  //           this._ellipsisDelay += 0.75f;
  //           ++this._ellipsisCount;
  //           if (this._ellipsisCount > 3)
  //             this._ellipsisCount = 1;
  //         }
  //       }
  //       else if (this.hasDrawn && this.completePause == -1)
  //       {
  //         Game1.game1.IsSaving = true;
  //         if (Game1.IsMasterGame)
  //         {
  //           if (Game1.saveOnNewDay)
  //           {
  //             this.loader = SaveGame.Save();
  //           }
  //           else
  //           {
  //             this.margin = -1;
  //             this.complete();
  //           }
  //         }
  //         else
  //         {
  //           NewSaveGameMenuV2.saveClientOptions();
  //           this.complete();
  //         }
  //       }
  //       if (this.completePause < 0)
  //         return;
  //       this.completePause -= time.ElapsedGameTime.Milliseconds;
  //       this.saveText.update(time);
  //       if (this.completePause >= 0)
  //         return;
  //       this.quit = true;
  //       this.completePause = -9999;
  //       if (((object) Game1.activeClickableMenu).Equals((object) this))
  //       {
  //         Game1.player.checkForLevelTenStatus();
  //         Game1.exitActiveMenu();
  //       }
  //       Game1.currentLocation.resetForPlayerEntry();
  //     }
  //   }
  //
  //   public static void saveClientOptions()
  //   {
  //     StartupPreferences startupPreferences = new StartupPreferences();
  //     int num1 = 0;
  //     int num2 = 1;
  //     startupPreferences.loadPreferences(num1 != 0, num2 != 0);
  //     Options options = Game1.options;
  //     startupPreferences.clientOptions = options;
  //     int num3 = 0;
  //     startupPreferences.savePreferences(num3 != 0);
  //   }
  //
  //   public virtual void draw(SpriteBatch b)
  //   {
  //     base.draw(b);
  //     Vector2 vector2 = Utility.makeSafe(
  //       new Vector2(
  //         64f,
  //         (float) (((Rectangle) ref Game1.viewport).Height - 64)
  //       ), 
  //       new Vector2(64f, 64f)
  //     );
  //     if (this.completePause >= 0)
  //     {
  //       if (Game1.saveOnNewDay)
  //         this.saveText.draw(b, vector2);
  //     }
  //     else if (this.margin < 0 || Game1.IsClient)
  //     {
  //       this._stringBuilder.Clear();
  //       for (int index = 0; index < this._ellipsisCount; ++index)
  //         this._stringBuilder.Append(".");
  //       b.DrawString(Game1.dialogueFont, this._stringBuilder, vector2, Color.White);
  //     }
  //     else
  //     {
  //       this._stringBuilder.Clear();
  //       this._stringBuilder.Append(Game1.content.LoadString("Strings\\StringsFromCSFiles:SaveGameMenu.cs.11381"));
  //       for (int index = 0; index < this._ellipsisCount; ++index)
  //         this._stringBuilder.Append(".");
  //       b.DrawString(Game1.dialogueFont, this._stringBuilder, vector2, Color.White);
  //     }
  //     this.hasDrawn = true;
  //   }
  //
  //   public void Dispose() => Game1.game1.IsSaving = false;
  // }
}
