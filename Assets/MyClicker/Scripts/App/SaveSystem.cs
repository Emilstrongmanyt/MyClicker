using System;
using System.IO;
using UnityEngine;

namespace MyClicker.App
{
    public class SaveSystem
    {
        readonly string _path;
        bool _pending;
        float _dirtyAt;

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
            PersistNow();
        }

        public void AddGold(double amount)
        {
            if (amount == 0d || double.IsNaN(amount))
                return;
            if (double.IsInfinity(amount))
                amount = 0d;
            Profile.gold = Math.Max(0d, Profile.gold + amount);
            MarkDirty();
        }

        public void AddDust(int amount)
        {
            if (amount == 0)
                return;
            Profile.dust = Mathf.Max(0, Profile.dust + amount);
            MarkDirty();
        }

        public bool TrySpendDust(int amount)
        {
            if (amount <= 0)
                return true;
            if (Profile.dust < amount)
                return false;
            Profile.dust -= amount;
            MarkDirty();
            return true;
        }

        public bool TrySpendGold(double amount)
        {
            if (amount <= 0d)
                return true;
            if (double.IsNaN(amount) || double.IsInfinity(amount) || Profile.gold < amount)
                return false;
            Profile.gold -= amount;
            MarkDirty();
            return true;
        }

        public void MarkDirty()
        {
            _pending = true;
            _dirtyAt = Time.realtimeSinceStartup;
            GameServices.Instance?.NotifyProfile();
        }

        public void Tick()
        {
            if (_pending && Time.realtimeSinceStartup - _dirtyAt >= 1.25f)
                PersistNow();
        }

        public void PersistNow()
        {
            _pending = false;
            Profile.lastSeenUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            try
            {
                File.WriteAllText(_path, JsonUtility.ToJson(Profile, true));
            }
            catch (Exception ex)
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
                    {
                        if (loaded.wave < 1)
                            loaded.wave = 1;
                        if (double.IsNaN(loaded.gold) || loaded.gold < 0d)
                            loaded.gold = 0d;
                        return loaded;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MyClicker] Load failed: " + ex.Message);
            }

            return new PlayerProfile();
        }
    }
}
