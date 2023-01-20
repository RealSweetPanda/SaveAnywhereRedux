using System;

namespace SaveAnywhere.Framework {
    public class SaveAnywhereAPI {
        
        public event EventHandler BeforeSave {
            add => SaveAnywhere.Instance.SaveManager.beforeSave += value;
            remove => SaveAnywhere.Instance.SaveManager.beforeSave -= value;
        }

        public event EventHandler AfterSave {
            add => SaveAnywhere.Instance.SaveManager.afterSave += value;
            remove => SaveAnywhere.Instance.SaveManager.afterSave -= value;
        }

        public event EventHandler AfterLoad {
            add => SaveAnywhere.Instance.SaveManager.afterLoad += value;
            remove => SaveAnywhere.Instance.SaveManager.afterLoad -= value;
        }

        public void addBeforeSaveEvent(string ID, Action BeforeSave) {
            SaveAnywhere.Instance.SaveManager.beforeCustomSavingBegins.Add(ID, BeforeSave);
        }

        public void removeBeforeSaveEvent(string ID, Action BeforeSave) {
            SaveAnywhere.Instance.SaveManager.beforeCustomSavingBegins.Remove(ID);
        }

        public void addAfterSaveEvent(string ID, Action AfterSave) {
            SaveAnywhere.Instance.SaveManager.afterCustomSavingCompleted.Add(ID, AfterSave);
        }

        public void removeAfterSaveEvent(string ID, Action AfterSave) {
            SaveAnywhere.Instance.SaveManager.afterCustomSavingCompleted.Remove(ID);
        }

        public void addAfterLoadEvent(string ID, Action AfterLoad) {
            SaveAnywhere.Instance.SaveManager.afterSaveLoaded.Add(ID, AfterLoad);
        }

        public void removeAfterLoadEvent(string ID, Action AfterLoad) {
            SaveAnywhere.Instance.SaveManager.afterSaveLoaded.Remove(ID);
        }
    }
}