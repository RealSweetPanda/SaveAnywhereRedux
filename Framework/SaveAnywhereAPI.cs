// Decompiled with JetBrains decompiler
// Type: Omegasis.SaveAnywhere.Framework.SaveAnywhereAPI
// Assembly: SaveAnywhere, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: CA1E3B07-AC71-4821-90DC-80822753C1D9
// Assembly location: C:\Users\keren\Desktop\SaveAnywhere1.5\SaveAnywhere.dll

using System;

namespace Omegasis.SaveAnywhere.Framework
{
  public class SaveAnywhereAPI
  {
    public event EventHandler BeforeSave
    {
      add => Omegasis.SaveAnywhere.SaveAnywhere.Instance.SaveManager.beforeSave += value;
      remove => Omegasis.SaveAnywhere.SaveAnywhere.Instance.SaveManager.beforeSave -= value;
    }

    public event EventHandler AfterSave
    {
      add => Omegasis.SaveAnywhere.SaveAnywhere.Instance.SaveManager.afterSave += value;
      remove => Omegasis.SaveAnywhere.SaveAnywhere.Instance.SaveManager.afterSave -= value;
    }

    public event EventHandler AfterLoad
    {
      add => Omegasis.SaveAnywhere.SaveAnywhere.Instance.SaveManager.afterLoad += value;
      remove => Omegasis.SaveAnywhere.SaveAnywhere.Instance.SaveManager.afterLoad -= value;
    }

    public void addBeforeSaveEvent(string ID, Action BeforeSave) => Omegasis.SaveAnywhere.SaveAnywhere.Instance.SaveManager.beforeCustomSavingBegins.Add(ID, BeforeSave);

    public void removeBeforeSaveEvent(string ID, Action BeforeSave) => Omegasis.SaveAnywhere.SaveAnywhere.Instance.SaveManager.beforeCustomSavingBegins.Remove(ID);

    public void addAfterSaveEvent(string ID, Action AfterSave) => Omegasis.SaveAnywhere.SaveAnywhere.Instance.SaveManager.afterCustomSavingCompleted.Add(ID, AfterSave);

    public void removeAfterSaveEvent(string ID, Action AfterSave) => Omegasis.SaveAnywhere.SaveAnywhere.Instance.SaveManager.afterCustomSavingCompleted.Remove(ID);

    public void addAfterLoadEvent(string ID, Action AfterLoad) => Omegasis.SaveAnywhere.SaveAnywhere.Instance.SaveManager.afterSaveLoaded.Add(ID, AfterLoad);

    public void removeAfterLoadEvent(string ID, Action AfterLoad) => Omegasis.SaveAnywhere.SaveAnywhere.Instance.SaveManager.afterSaveLoaded.Remove(ID);
  }
}
