using System.IO;
using UnityEngine;

namespace MyClicker.App
{
    public class SaveSystem
    {
        readonly string _path;

        public PlayerProfile Profile { get; private set; }

        public bool HasCharacter => Profile != null && Profile.hasCharacter;

        public SaveSystem()
        {
            _path = Path.Combine(Application.persistentDataPath, "myclicker-profile.json");
            Profile = Load();
        }

        public void MarkCharacterCreated()
        {
            Profile.hasCharacter = true;
            Persist();
        }

        public void AddGold(int amount)
        {
            Profile.gold = Mathf.Max(0, Profile.gold + amount);
            Persist();
        }

        public void Persist()
        {
            try
            {
                File.WriteAllText(_path, JsonUtility.ToJson(Profile, true));
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[MyClicker] Save failed: " + ex.Message);
            }
        }

        PlayerProfile Load()
        {
            try
            {
                if (File.Exists(_path))
                {
                    var loaded = JsonUtility.FromJson<PlayerProfile>(File.ReadAllText(_path));
                    if (loaded != null)
                        return loaded;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[MyClicker] Load failed: " + ex.Message);
            }

            return new PlayerProfile();
        }
    }
}
