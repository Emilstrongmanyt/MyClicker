using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyClicker.Data
{
    [CreateAssetMenu(menuName = "MyClicker/Content Catalog", fileName = "ContentCatalog")]
    public class ContentCatalog : ScriptableObject
    {
        public const string ResourceName = "ContentCatalog";

        public UnitVisual[] enemies = Array.Empty<UnitVisual>();
        public UnitVisual[] bosses = Array.Empty<UnitVisual>();
        public ZoneDef[] zones = Array.Empty<ZoneDef>();
        public UpgradeDef[] upgrades = Array.Empty<UpgradeDef>();
        public PotionDef[] potions = Array.Empty<PotionDef>();
        public IconLibrary icons = new IconLibrary();
        public AudioLibrary audio = new AudioLibrary();

        public static ContentCatalog Load()
        {
            var catalog = Resources.Load<ContentCatalog>(ResourceName);
            if (catalog == null)
                catalog = CreateInstance<ContentCatalog>();
            catalog.EnsureDefaults();
            return catalog;
        }

        public void EnsureDefaults()
        {
            if (icons == null)
                icons = new IconLibrary();
            if (audio == null)
                audio = new AudioLibrary();
            upgrades = MergeUpgrades(upgrades);
            if (potions == null || potions.Length == 0)
                potions = DefaultPotions();
            if (zones == null || zones.Length == 0)
                zones = new[] { ZoneDef.Fallback };
            if (enemies == null)
                enemies = Array.Empty<UnitVisual>();
            if (bosses == null)
                bosses = Array.Empty<UnitVisual>();
        }

        static UpgradeDef[] MergeUpgrades(UpgradeDef[] existing)
        {
            var defaults = DefaultUpgrades();
            if (existing == null || existing.Length == 0)
                return defaults;

            var merged = new List<UpgradeDef>(existing);
            for (int i = 0; i < defaults.Length; i++)
            {
                bool found = false;
                for (int j = 0; j < merged.Count; j++)
                {
                    if (merged[j] != null && merged[j].id == defaults[i].id)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    merged.Add(defaults[i]);
            }

            return merged.ToArray();
        }

        static UpgradeDef[] DefaultUpgrades()
        {
            return new[]
            {
                new UpgradeDef { id = ContentIds.Might, displayName = "Might", description = "Tap and auto damage.", baseCost = 15, costGrowth = 1.18f, perLevel = 4f, requiresId = "", requiresLevel = 0 },
                new UpgradeDef { id = ContentIds.Fortune, displayName = "Fortune", description = "More gold from every kill.", baseCost = 25, costGrowth = 1.20f, perLevel = 0.12f },
                new UpgradeDef { id = ContentIds.Swift, displayName = "Swift", description = "Faster automatic swings.", baseCost = 40, costGrowth = 1.22f, perLevel = 0.07f },
                new UpgradeDef { id = ContentIds.Crit, displayName = "Crit", description = "Chance for triple damage.", baseCost = 50, costGrowth = 1.25f, perLevel = 0.02f },
                new UpgradeDef { id = ContentIds.Cleave, displayName = "Cleave", description = "Strikes splash to a nearby foe.", baseCost = 80, costGrowth = 1.23f, perLevel = 0.05f, requiresId = ContentIds.Might, requiresLevel = 6 },
                new UpgradeDef { id = ContentIds.Fury, displayName = "Fury", description = "Critical hits hit even harder.", baseCost = 90, costGrowth = 1.26f, perLevel = 0.25f, requiresId = ContentIds.Crit, requiresLevel = 5 },
                new UpgradeDef { id = ContentIds.Harvest, displayName = "Harvest", description = "More dust and potion drops.", baseCost = 70, costGrowth = 1.22f, perLevel = 0.04f, requiresId = ContentIds.Fortune, requiresLevel = 6 },
            };
        }

        static PotionDef[] DefaultPotions()
        {
            return new[]
            {
                new PotionDef { id = ContentIds.PotMight, displayName = "Ember Vial", description = "Hold to read. Drink for +60% tap damage for 20 seconds.", duration = 20f, potency = 0.6f },
                new PotionDef { id = ContentIds.PotSwift, displayName = "Gale Tonic", description = "Hold to read. Drink to speed auto-swings by 35% for 20 seconds.", duration = 20f, potency = 0.35f },
                new PotionDef { id = ContentIds.PotGold, displayName = "Gilded Brew", description = "Hold to read. Drink to double gold from kills for 20 seconds.", duration = 20f, potency = 1f },
            };
        }

        public UnitVisual FindEnemy(string id) => Find(enemies, id);
        public UnitVisual FindBoss(string id) => Find(bosses, id);

        public UnitVisual FindUnit(string id)
        {
            var unit = FindEnemy(id);
            return unit ?? FindBoss(id);
        }

        public ZoneDef ZoneAt(int index)
        {
            if (zones == null || zones.Length == 0)
                return ZoneDef.Fallback;
            int i = Mathf.Clamp(index, 0, zones.Length - 1);
            return zones[i] ?? ZoneDef.Fallback;
        }

        public UpgradeDef FindUpgrade(string id)
        {
            if (upgrades == null)
                return null;
            for (int i = 0; i < upgrades.Length; i++)
            {
                if (upgrades[i] != null && upgrades[i].id == id)
                    return upgrades[i];
            }

            return null;
        }

        public PotionDef FindPotion(string id)
        {
            if (potions == null)
                return null;
            for (int i = 0; i < potions.Length; i++)
            {
                if (potions[i] != null && potions[i].id == id)
                    return potions[i];
            }

            return null;
        }

        public UnitVisual PickEnemy(ZoneDef zone, int wave)
        {
            if (zone != null && zone.enemyIds != null && zone.enemyIds.Length > 0)
            {
                string id = zone.enemyIds[Mathf.Abs(wave - 1) % zone.enemyIds.Length];
                var visual = FindEnemy(id);
                if (visual != null)
                    return visual;
            }

            if (enemies != null && enemies.Length > 0)
                return enemies[Mathf.Abs(wave - 1) % enemies.Length];
            return null;
        }

        static UnitVisual Find(UnitVisual[] list, string id)
        {
            if (list == null || string.IsNullOrEmpty(id))
                return null;
            for (int i = 0; i < list.Length; i++)
            {
                if (list[i] != null && list[i].id == id)
                    return list[i];
            }

            return null;
        }
    }

    public static class ContentIds
    {
        public const string Might = "might";
        public const string Fortune = "fortune";
        public const string Swift = "swift";
        public const string Crit = "crit";
        public const string Cleave = "cleave";
        public const string Fury = "fury";
        public const string Harvest = "harvest";

        public const string PotMight = "pot_might";
        public const string PotSwift = "pot_swift";
        public const string PotGold = "pot_gold";

        public const string MutMight = "mut_might";
        public const string MutFortune = "mut_fortune";
        public const string MutSwift = "mut_swift";
        public const string MutLuck = "mut_luck";
    }

    [Serializable]
    public class UnitVisual
    {
        public string id;
        public string displayName;
        public bool isBoss;
        public float scale = 2.1f;
        public Sprite[] idle;
        public Sprite[] walk;
        public Sprite[] attack;
        public Sprite[] hurt;
        public Sprite[] death;

        public Sprite Preview
        {
            get
            {
                if (idle != null && idle.Length > 0) return idle[0];
                if (walk != null && walk.Length > 0) return walk[0];
                return null;
            }
        }

        public Sprite[] Clip(UnitClip clip)
        {
            switch (clip)
            {
                case UnitClip.Walk: return FirstLive(walk, idle);
                case UnitClip.Attack: return FirstLive(attack, idle);
                case UnitClip.Hurt: return FirstLive(hurt, idle);
                case UnitClip.Death: return FirstLive(death, hurt, idle);
                default: return FirstLive(idle, walk);
            }
        }

        static Sprite[] FirstLive(params Sprite[][] options)
        {
            for (int i = 0; i < options.Length; i++)
            {
                if (options[i] != null && options[i].Length > 0)
                    return options[i];
            }

            return Array.Empty<Sprite>();
        }
    }

    public enum UnitClip
    {
        Idle,
        Walk,
        Attack,
        Hurt,
        Death
    }

    [Serializable]
    public class ZoneDef
    {
        public string id;
        public string displayName;
        public string[] enemyIds;
        public string bossId;
        public float hpMul = 1f;
        public float goldMul = 1f;
        public Sprite background;
        public string battleCue = "battle";
        public string bossCue = "boss";

        public static ZoneDef Fallback => new ZoneDef
        {
            id = "old-road",
            displayName = "The Old Road",
            enemyIds = Array.Empty<string>(),
            bossId = "boss_01",
            hpMul = 1f,
            goldMul = 1f
        };
    }

    [Serializable]
    public class UpgradeDef
    {
        public string id;
        public string displayName;
        public string description;
        public Sprite icon;
        public int baseCost = 15;
        public float costGrowth = 1.18f;
        public float perLevel = 4f;
        public int maxLevel = 200;
        public string requiresId = "";
        public int requiresLevel;
    }

    [Serializable]
    public class PotionDef
    {
        public string id;
        public string displayName;
        public string description;
        public Sprite icon;
        public float duration = 20f;
        public float potency = 0.5f;
    }

    [Serializable]
    public class IconLibrary
    {
        public Sprite gold;
        public Sprite dust;
        public Sprite glory;
        public Sprite shop;
        public Sprite might;
        public Sprite fortune;
        public Sprite swift;
        public Sprite crit;
        public Sprite potion;
        public Sprite settings;
        public Sprite heart;
        public Sprite skull;
        public Sprite anvil;
        public Sprite chest;
        public Sprite lockIcon;
    }

    [Serializable]
    public class AudioLibrary
    {
        public AudioClip create;
        public AudioClip battle;
        public AudioClip boss;
        public AudioClip night;
        public AudioClip day2;
        public AudioClip day3;
        public AudioClip night2;
        public AudioClip night3;
    }
}
