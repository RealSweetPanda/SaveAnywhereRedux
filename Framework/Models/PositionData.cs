// Decompiled with JetBrains decompiler
// Type: Omegasis.SaveAnywhere.Framework.Models.CharacterData
// Assembly: SaveAnywhere, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: CA1E3B07-AC71-4821-90DC-80822753C1D9
// Assembly location: C:\Users\keren\Desktop\SaveAnywhere1.5\SaveAnywhere.dll

using Microsoft.Xna.Framework;

namespace Omegasis.SaveAnywhere.Framework.Models
{
  public class CharacterData
  {
    public CharacterType Type { get; set; }

    public string Name { get; set; }

    public string Map { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public int FacingDirection { get; set; }

    public CharacterData()
    {
    }

    public CharacterData(
      CharacterType type,
      string name,
      string map,
      int x,
      int y,
      int facingDirection)
    {
      this.Type = type;
      this.Name = name;
      this.Map = map;
      this.X = x;
      this.Y = y;
      this.FacingDirection = facingDirection;
    }

    public CharacterData(
      CharacterType type,
      string name,
      string map,
      Point tile,
      int facingDirection)
      : this(type, name, map, tile.X, tile.Y, facingDirection)
    {
    }
  }
}
