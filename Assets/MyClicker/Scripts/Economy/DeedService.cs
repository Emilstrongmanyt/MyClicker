using System.Collections.Generic;
using MyClicker.App;
using MyClicker.Combat;
using MyClicker.Data;
using UnityEngine;

namespace MyClicker.Economy
{
    public class DeedService
    {
        public const float RenownPerDeed = 0.02f;

        readonly GameServices _services;
        readonly List<string> _fresh = new List<string>();

        public DeedService(GameServices services)
        {
            _services = services;
        }

        PlayerProfile Profile => _services.Save.Profile;

        public static readonly DeedDef[] All =
        {
            new DeedDef { id = "kills_100", title = "First Blood", hint = "Slay 100 foes.", kind = DeedKind.Kills, need = 100 },
            new DeedDef { id = "kills_500", title = "Horde Breaker", hint = "Slay 500 foes.", kind = DeedKind.Kills, need = 500 },
            new DeedDef { id = "kills_1000", title = "Butcher", hint = "Slay 1,000 foes.", kind = DeedKind.Kills, need = 1000 },
            new DeedDef { id = "kills_5000", title = "Reaper", hint = "Slay 5,000 foes.", kind = DeedKind.Kills, need = 5000 },
            new DeedDef { id = "kills_10000", title = "Endless Blade", hint = "Slay 10,000 foes.", kind = DeedKind.Kills, need = 10000 },
            new DeedDef { id = "boss_1", title = "Giant Killer", hint = "Defeat a boss.", kind = DeedKind.Bosses, need = 1 },
            new DeedDef { id = "boss_10", title = "Slayer", hint = "Defeat 10 bosses.", kind = DeedKind.Bosses, need = 10 },
            new DeedDef { id = "boss_50", title = "Dreadbane", hint = "Defeat 50 bosses.", kind = DeedKind.Bosses, need = 50 },
            new DeedDef { id = "boss_200", title = "Myth Eater", hint = "Defeat 200 bosses.", kind = DeedKind.Bosses, need = 200 },
            new DeedDef { id = "zone_1", title = "Moon Fen", hint = "Reach Moon Fen.", kind = DeedKind.BestZone, need = 1 },
            new DeedDef { id = "zone_3", title = "Labyrinth Gate", hint = "Reach Labyrinth Gate.", kind = DeedKind.BestZone, need = 3 },
            new DeedDef { id = "zone_6", title = "Bone Yard", hint = "Reach Bone Yard.", kind = DeedKind.BestZone, need = 6 },
            new DeedDef { id = "zone_9", title = "Harvest Night", hint = "Reach Harvest Night.", kind = DeedKind.BestZone, need = 9 },
            new DeedDef { id = "endless_1", title = "Endless Road", hint = "Loop the map once.", kind = DeedKind.BestCycle, need = 1 },
            new DeedDef { id = "endless_3", title = "Thrice Around", hint = "Reach Endless cycle 3.", kind = DeedKind.BestCycle, need = 3 },
            new DeedDef { id = "endless_5", title = "Deep Cycle", hint = "Reach Endless cycle 5.", kind = DeedKind.BestCycle, need = 5 },
            new DeedDef { id = "ascend_1", title = "First Ascent", hint = "Ascend once.", kind = DeedKind.Ascend, need = 1 },
            new DeedDef { id = "ascend_3", title = "Thrice Returned", hint = "Ascend 3 times.", kind = DeedKind.Ascend, need = 3 },
            new DeedDef { id = "ascend_10", title = "Cycle Walker", hint = "Ascend 10 times.", kind = DeedKind.Ascend, need = 10 },
            new DeedDef { id = "relic_1", title = "First Relic", hint = "Find a relic.", kind = DeedKind.Relics, need = 1 },
            new DeedDef { id = "relic_4", title = "Collector", hint = "Own 4 relics.", kind = DeedKind.Relics, need = 4 },
            new DeedDef { id = "relic_8", title = "Hoard", hint = "Own 8 relics.", kind = DeedKind.Relics, need = 8 },
            new DeedDef { id = "relic_12", title = "Archive", hint = "Own 12 relics.", kind = DeedKind.Relics, need = 12 },
            new DeedDef { id = "forge_10", title = "Apprentice", hint = "Buy 10 Forge ranks.", kind = DeedKind.ForgeBought, need = 10 },
            new DeedDef { id = "forge_50", title = "Smith", hint = "Buy 50 Forge ranks.", kind = DeedKind.ForgeBought, need = 50 },
            new DeedDef { id = "forge_100", title = "Master Smith", hint = "Buy 100 Forge ranks.", kind = DeedKind.ForgeBought, need = 100 },
            new DeedDef { id = "slam", title = "Slam", hint = "Use Slam.", kind = DeedKind.Slam, need = 1 },
            new DeedDef { id = "fury", title = "Fury", hint = "Use Fury.", kind = DeedKind.Fury, need = 1 },
            new DeedDef { id = "sweep", title = "Sweep", hint = "Use Sweep.", kind = DeedKind.Sweep, need = 1 },
            new DeedDef { id = "potion", title = "Taster", hint = "Drink a potion.", kind = DeedKind.Potion, need = 1 },
            new DeedDef { id = "temper", title = "Tempered", hint = "Temper a relic.", kind = DeedKind.Temper, need = 1 },
            new DeedDef { id = "might_20", title = "Iron Arm", hint = "Reach Might 20.", kind = DeedKind.Might, need = 20 },
            new DeedDef { id = "swift_max", title = "Overclocked", hint = "Max Swift.", kind = DeedKind.SwiftMax, need = 1 },
        };

        public int Count
        {
            get
            {
                return Profile.unlockedDeeds != null ? Profile.unlockedDeeds.Length : 0;
            }
        }

        public float Renown
        {
            get
            {
                float value = Count * RenownPerDeed;
                if (GloryTree.Has(Profile, GloryIds.DeedAngel))
                    value *= 1.5f;
                return value;
            }
        }

        public bool Has(string id)
        {
            if (string.IsNullOrEmpty(id) || Profile.unlockedDeeds == null)
                return false;
            for (int i = 0; i < Profile.unlockedDeeds.Length; i++)
            {
                if (Profile.unlockedDeeds[i] == id)
                    return true;
            }

            return false;
        }

        public int Current(DeedDef deed)
        {
            if (deed == null)
                return 0;
            switch (deed.kind)
            {
                case DeedKind.Kills: return Profile.kills;
                case DeedKind.Bosses: return Profile.bossesSlain;
                case DeedKind.BestZone: return Profile.bestZone;
                case DeedKind.Ascend: return Profile.ascendCount;
                case DeedKind.Relics: return Profile.unlockedGear != null ? Profile.unlockedGear.Length : 0;
                case DeedKind.ForgeBought: return Profile.forgeBought;
                case DeedKind.Slam: return Profile.usedSlam ? 1 : 0;
                case DeedKind.Fury: return Profile.usedFury ? 1 : 0;
                case DeedKind.Sweep: return Profile.usedSweep ? 1 : 0;
                case DeedKind.Potion: return Profile.usedPotion ? 1 : 0;
                case DeedKind.Temper: return Profile.usedTemper ? 1 : 0;
                case DeedKind.Might: return Profile.mightLevel;
                case DeedKind.SwiftMax: return _services.Economy != null && _services.Economy.IsMaxed(ContentIds.Swift) ? 1 : 0;
                case DeedKind.BestCycle: return Profile.bestCycle;
                default: return 0;
            }
        }

        public void Evaluate()
        {
            _fresh.Clear();
            for (int i = 0; i < All.Length; i++)
            {
                var deed = All[i];
                if (deed == null || Has(deed.id))
                    continue;
                if (Current(deed) < deed.need)
                    continue;
                if (!Unlock(deed.id))
                    continue;
                _fresh.Add(deed.title);
            }

            if (_fresh.Count == 0)
                return;
            _services.Save.MarkDirty();
            var battle = Object.FindFirstObjectByType<TapCombatController>();
            string line = _fresh.Count == 1
                ? "Deed  " + _fresh[0]
                : "Deeds  " + _fresh[0] + "  +" + (_fresh.Count - 1);
            battle?.Announce(line, 2.2f, false);
        }

        bool Unlock(string id)
        {
            if (Has(id))
                return false;
            int n = Profile.unlockedDeeds != null ? Profile.unlockedDeeds.Length : 0;
            var next = new string[n + 1];
            if (n > 0)
                System.Array.Copy(Profile.unlockedDeeds, next, n);
            next[n] = id;
            Profile.unlockedDeeds = next;
            return true;
        }
    }

    public enum DeedKind
    {
        Kills,
        Bosses,
        BestZone,
        Ascend,
        Relics,
        ForgeBought,
        Slam,
        Fury,
        Sweep,
        Potion,
        Temper,
        Might,
        SwiftMax,
        BestCycle
    }

    [System.Serializable]
    public class DeedDef
    {
        public string id;
        public string title;
        public string hint;
        public DeedKind kind;
        public int need = 1;
    }
}
