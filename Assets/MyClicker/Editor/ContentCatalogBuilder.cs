using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MyClicker.Data;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MyClicker.Editor
{
    [InitializeOnLoad]
    public sealed class ContentCatalogBuilder : IPreprocessBuildWithReport
    {
        const string CatalogPath = "Assets/MyClicker/Resources/ContentCatalog.asset";

        static readonly string[] EnemyNames =
        {
            "Night Spore", "Scarab Grub", "Pale Puff", "Cave Crawler", "Ash Imp", "Azure Blob",
            "Fen Tick", "Bog Wisp", "Reed Bat", "Fern Cat", "Mire Toad", "Tide Slime",
            "Ember Mite", "Soot Imp", "Cinder Bat", "Magma Grub", "Ash Wolf", "Coal Golem",
            "Frost Bit", "Ice Imp", "Snow Bat", "Hail Wolf", "Glacier Slime", "Rime Knight"
        };

        static readonly (string id, string name, string[] enemies, string boss, float hp, float gold)[] Zones =
        {
            ("old-road", "The Old Road", Ids(1, 6), "boss_01", 1.00f, 1.00f),
            ("moon-fen", "Moon Fen", Ids(7, 12), "boss_02", 1.25f, 1.20f),
            ("chitter-deep", "Chitter Deep", Ids(13, 18), "boss_03", 1.50f, 1.40f),
            ("labyrinth-gate", "Labyrinth Gate", Ids(19, 24), "boss_04", 1.80f, 1.65f),
            ("blight-ducts", "Blight Ducts", Ids(1, 6), "boss_05", 2.15f, 1.90f),
            ("howling-wood", "Howling Wood", Ids(7, 12), "boss_06", 2.55f, 2.20f),
            ("bone-yard", "Bone Yard", Ids(13, 18), "boss_07", 3.05f, 2.55f),
            ("black-pool", "Black Pool", Ids(19, 24), "boss_08", 3.60f, 2.95f),
            ("titan-stair", "Titan Stair", Ids(5, 12), "boss_09", 4.30f, 3.40f),
            ("harvest-night", "Harvest Night", Ids(1, 24), "boss_10", 5.10f, 4.00f),
        };

        public int callbackOrder => 40;

        static ContentCatalogBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                var catalog = AssetDatabase.LoadAssetAtPath<ContentCatalog>(CatalogPath);
                if (catalog == null || catalog.enemies == null || catalog.enemies.Length == 0)
                    Rebuild();
            };
        }

        public static void BatchRebuild()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Rebuild();
        }

        [MenuItem("MyClicker/Rebuild Content Catalog")]
        public static void Rebuild()
        {
            var catalog = LoadOrCreate();
            catalog.enemies = BuildEnemies();
            catalog.bosses = BuildBosses();
            catalog.icons = BuildIcons();
            catalog.audio = BuildAudio();
            catalog.upgrades = BuildUpgrades(catalog.icons);
            catalog.potions = BuildPotions();
            catalog.zones = BuildZones();
            var config = AssetDatabase.LoadAssetAtPath<MyClicker.Data.GameConfig>("Assets/MyClicker/Resources/GameConfig.asset");
            if (config != null)
            {
                config.world.backgroundSprites = AllBackgroundSlices();
                AssignCainosTiles(config.world);
                EditorUtility.SetDirty(config);
            }
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log("[MyClicker] Content catalog rebuilt: " +
                      catalog.enemies.Length + " enemies, " +
                      catalog.bosses.Length + " bosses, " +
                      catalog.zones.Length + " zones.");
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            Rebuild();
        }

        static ContentCatalog LoadOrCreate()
        {
            if (!AssetDatabase.IsValidFolder("Assets/MyClicker/Resources"))
                AssetDatabase.CreateFolder("Assets/MyClicker", "Resources");
            var existing = AssetDatabase.LoadAssetAtPath<ContentCatalog>(CatalogPath);
            if (existing != null)
                return existing;
            var created = ScriptableObject.CreateInstance<ContentCatalog>();
            AssetDatabase.CreateAsset(created, CatalogPath);
            return created;
        }

        static UnitVisual[] BuildEnemies()
        {
            var list = new List<UnitVisual>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/ElvAssets/Enemies" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var match = Regex.Match(Path.GetFileNameWithoutExtension(path), @"Enemy_(\d+)_A$", RegexOptions.IgnoreCase);
                if (!match.Success)
                    continue;
                int index = int.Parse(match.Groups[1].Value);
                var visual = FromSheet(
                    "enemy_" + index.ToString("000"),
                    index >= 1 && index <= EnemyNames.Length ? EnemyNames[index - 1] : "Invader " + index,
                    path,
                    boss: false,
                    scale: 2.25f);
                if (visual != null)
                    list.Add(visual);
            }

            return list.OrderBy(v => v.id).ToArray();
        }

        static UnitVisual[] BuildBosses()
        {
            var list = new List<UnitVisual>();
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/ElvAssets/Bosses" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file = Path.GetFileNameWithoutExtension(path);
                var numbered = Regex.Match(file, @"Boss0?(\d+)_A$", RegexOptions.IgnoreCase);
                var halloween = Regex.Match(file, @"Boss_Halloween_A$", RegexOptions.IgnoreCase);
                if (!numbered.Success && !halloween.Success)
                    continue;

                int index = halloween.Success ? 10 : int.Parse(numbered.Groups[1].Value);
                string folder = Path.GetFileName(Path.GetDirectoryName(path) ?? "");
                string name = folder;
                int dash = folder.IndexOf('-');
                if (dash >= 0 && dash + 1 < folder.Length)
                    name = folder.Substring(dash + 1).Trim();
                if (string.IsNullOrEmpty(name))
                    name = "Boss " + index;
                if (name.Equals("TItan Guard", StringComparison.OrdinalIgnoreCase))
                    name = "Titan Guard";

                var visual = FromSheet("boss_" + index.ToString("00"), name, path, boss: true, scale: 2.7f);
                if (visual != null)
                    list.Add(visual);
            }

            return list.OrderBy(v => v.id).ToArray();
        }

        static UnitVisual FromSheet(string id, string displayName, string path, bool boss, float scale)
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(IndexOf)
                .ToArray();
            if (sprites.Length == 0)
                return null;

            var rows = SplitRows(sprites);
            var visual = new UnitVisual
            {
                id = id,
                displayName = displayName,
                isBoss = boss,
                scale = scale
            };
            AssignClips(visual, rows);
            return visual;
        }

        static void AssignClips(UnitVisual visual, List<Sprite[]> rows)
        {
            if (rows.Count == 0)
                return;
            visual.idle = rows[0];
            visual.walk = rows.Count > 1 ? rows[1] : rows[0];
            visual.death = rows[rows.Count - 1];
            visual.hurt = rows.Count >= 3 ? rows[rows.Count - 2] : visual.idle;
            if (rows.Count >= 4)
                visual.attack = rows[rows.Count >= 6 ? rows.Count - 3 : 2];
            else
                visual.attack = visual.idle;
        }

        static List<Sprite[]> SplitRows(Sprite[] sprites)
        {
            var groups = new SortedDictionary<int, List<Sprite>>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
            foreach (var sprite in sprites)
            {
                int y = Mathf.RoundToInt(sprite.rect.y);
                if (!groups.TryGetValue(y, out var row))
                {
                    row = new List<Sprite>();
                    groups.Add(y, row);
                }

                row.Add(sprite);
            }

            var rows = new List<Sprite[]>();
            foreach (var pair in groups)
                rows.Add(pair.Value.OrderBy(s => s.rect.x).ToArray());
            return rows;
        }

        static Sprite[] AllBackgroundSlices()
        {
            const string path = "Assets/2D Casual backgorund/Sprite/Asset4u_HD.png";
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(IndexOf)
                .ToArray();
        }

        static void AssignCainosTiles(GameConfig.WorldSettings world)
        {
            const string tex = "Assets/Cainos/Pixel Art Top Down - Basic/Texture/";
            world.grassTiles = LoadSprites(tex + "TX Tileset Grass.png");
            world.stoneTiles = LoadSprites(tex + "TX Tileset Stone Ground.png");
            world.wallTiles = LoadSprites(tex + "TX Tileset Wall.png");
            world.plantSprites = LoadSprites(tex + "TX Plant.png");
            world.propSprites = LoadSprites(tex + "TX Props.png")
                .Concat(LoadSprites(tex + "TX Struct.png"))
                .ToArray();
        }

        static Sprite[] LoadSprites(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(s => s.name)
                .ToArray();
        }

        static ZoneDef[] BuildZones()
        {
            var bg = FirstSprite("Asset4u_HD", "2D Casual backgorund/Sprite");
            var list = new List<ZoneDef>();
            foreach (var zone in Zones)
            {
                list.Add(new ZoneDef
                {
                    id = zone.id,
                    displayName = zone.name,
                    enemyIds = zone.enemies,
                    bossId = zone.boss,
                    hpMul = zone.hp,
                    goldMul = zone.gold,
                    background = bg,
                    battleCue = zone.id == "moon-fen" || zone.id == "black-pool" || zone.id == "harvest-night"
                        ? "night"
                        : "battle",
                    bossCue = "boss"
                });
            }

            return list.ToArray();
        }

        static IconLibrary BuildIcons()
        {
            const string item = "Assets/Layer Lab/GUI-TheStone/ResourcesData/Sprites/Components/Icon_ItemIcons/ItemIcon_64";
            const string picto = "Assets/Layer Lab/GUI-TheStone/ResourcesData/Sprites/Components/Icon_PictoIcons/PictoIcon_64";
            const string chest = "Assets/Layer Lab/GUI-TheStone/ResourcesData/Sprites/Components/ChestLuckyBox";
            return new IconLibrary
            {
                gold = SpriteAt(item, "Icon_ItemIcon_Gold") ?? SpriteNamed("Icon_PictoIcon_Gold"),
                dust = SpriteAt(item, "Icon_ItemIcon_Purplegem") ?? SpriteNamed("Icon_PictoIcon_Gem"),
                glory = SpriteAt(item, "Icon_ItemIcon_Laurel") ?? SpriteAt(item, "Icon_ItemIcon_Trophy"),
                shop = SpriteAt(picto, "Icon_PictoIcon_Shop") ?? SpriteAt(item, "Icon_ItemIcon_Shop"),
                might = SpriteAt(item, "Icon_ItemIcon_Sword_A") ?? SpriteAt(picto, "Icon_PictoIcon_Sword"),
                fortune = SpriteAt(item, "Icon_ItemIcon_Clover") ?? SpriteAt(item, "Icon_ItemIcon_Gold"),
                swift = SpriteAt(item, "Icon_ItemIcon_Talaria") ?? SpriteAt(picto, "Icon_PictoIcon_Time"),
                crit = SpriteAt(item, "Icon_ItemIcon_Target") ?? SpriteAt(picto, "Icon_PictoIcon_Target"),
                potion = SpriteAt(item, "Icon_ItemIcon_Potion_Red") ?? SpriteAt(picto, "Icon_PictoIcon_Flask_01"),
                settings = SpriteAt(picto, "Icon_PictoIcon_Setting_1") ?? SpriteAt(item, "Icon_ItemIcon_Setting"),
                heart = SpriteAt(picto, "Icon_PictoIcon_Heart"),
                skull = SpriteAt(item, "Icon_ItemIcon_Skull") ?? SpriteAt(picto, "Icon_PictoIcon_Skull"),
                anvil = SpriteAt(item, "Icon_ItemIcon_Anvil") ?? SpriteAt(picto, "Icon_PictoIcon_Hammer"),
                chest = SpriteAt(chest, "Chest_Luckybox_Gold") ?? SpriteAt(item, "Icon_ItemIcon_Treasure"),
                lockIcon = SpriteAt(item, "Icon_ItemIcon_Lock") ?? SpriteAt(picto, "Icon_PictoIcon_Lock"),
            };
        }

        static AudioLibrary BuildAudio()
        {
            return new AudioLibrary
            {
                create = Clip("ES_Vagabond's Awakening - Dian Shuai"),
                battle = Clip("ES_Dawn of the Long Road - Dian Shuai"),
                boss = Clip("ES_Return of the Longship - Dian Shuai"),
                night = Clip("ES_Beneath the Old Moon - Adriel Fair"),
            };
        }

        static UpgradeDef[] BuildUpgrades(IconLibrary icons)
        {
            return new[]
            {
                new UpgradeDef
                {
                    id = ContentIds.Might,
                    displayName = "Might",
                    description = "Each rank adds tap and auto damage immediately.",
                    icon = icons.might,
                    baseCost = 15,
                    costGrowth = 1.18f,
                    perLevel = 4f
                },
                new UpgradeDef
                {
                    id = ContentIds.Fortune,
                    displayName = "Fortune",
                    description = "More gold from every kill.",
                    icon = icons.fortune,
                    baseCost = 25,
                    costGrowth = 1.20f,
                    perLevel = 0.12f
                },
                new UpgradeDef
                {
                    id = ContentIds.Swift,
                    displayName = "Swift",
                    description = "Your hero swings on their own, faster each rank.",
                    icon = icons.swift,
                    baseCost = 40,
                    costGrowth = 1.22f,
                    perLevel = 0.07f,
                    maxLevel = 40
                },
                new UpgradeDef
                {
                    id = ContentIds.Crit,
                    displayName = "Crit",
                    description = "Chance for a triple-damage strike.",
                    icon = icons.crit,
                    baseCost = 50,
                    costGrowth = 1.25f,
                    perLevel = 0.02f,
                    maxLevel = 30
                },
                new UpgradeDef
                {
                    id = ContentIds.Cleave,
                    displayName = "Cleave",
                    description = "Strikes splash to a nearby foe.",
                    icon = icons.skull != null ? icons.skull : icons.might,
                    baseCost = 80,
                    costGrowth = 1.23f,
                    perLevel = 0.05f,
                    maxLevel = 13,
                    requiresId = ContentIds.Might,
                    requiresLevel = 6
                },
                new UpgradeDef
                {
                    id = ContentIds.Fury,
                    displayName = "Fury",
                    description = "Critical hits hit even harder.",
                    icon = SpriteNamed("Icon_PictoIcon_Fire") ?? SpriteNamed("Icon_PictoIcon_Battle") ?? icons.crit,
                    baseCost = 90,
                    costGrowth = 1.26f,
                    perLevel = 0.25f,
                    requiresId = ContentIds.Crit,
                    requiresLevel = 5
                },
                new UpgradeDef
                {
                    id = ContentIds.Harvest,
                    displayName = "Harvest",
                    description = "More dust and potion drops.",
                    icon = icons.dust != null ? icons.dust : icons.fortune,
                    baseCost = 70,
                    costGrowth = 1.22f,
                    perLevel = 0.04f,
                    requiresId = ContentIds.Fortune,
                    requiresLevel = 6
                },
            };
        }

        static PotionDef[] BuildPotions()
        {
            return new[]
            {
                new PotionDef
                {
                    id = ContentIds.PotMight,
                    displayName = "Ember Vial",
                    description = "A warm draught. +60% tap damage for 20 seconds.",
                    icon = PotionSprite("pot3red") ?? PotionSprite("pot1red"),
                    duration = 20f,
                    potency = 0.6f
                },
                new PotionDef
                {
                    id = ContentIds.PotSwift,
                    displayName = "Gale Tonic",
                    description = "A brisk tonic. Auto-swings 35% faster for 20 seconds.",
                    icon = PotionSprite("pot8sky") ?? PotionSprite("pot3blue") ?? PotionSprite("pot1sky"),
                    duration = 20f,
                    potency = 0.35f
                },
                new PotionDef
                {
                    id = ContentIds.PotGold,
                    displayName = "Gilded Brew",
                    description = "A lucky brew. Double gold from every kill for 20 seconds.",
                    icon = PotionSprite("pot5yellow") ?? PotionSprite("pot1yellow"),
                    duration = 20f,
                    potency = 1f
                },
            };
        }

        static AudioClip Clip(string resourceName)
        {
            string path = "Assets/MyClicker/Resources/" + resourceName + ".mp3";
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        static Sprite SpriteAt(string folder, string name)
        {
            string[] exts = { ".Png", ".png" };
            foreach (var ext in exts)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(folder + "/" + name + ext);
                if (sprite != null)
                    return sprite;
            }

            return SpriteNamed(name);
        }

        static Sprite SpriteNamed(string name)
        {
            foreach (var guid in AssetDatabase.FindAssets(name + " t:Sprite"))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guid));
                if (sprite != null && sprite.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    return sprite;
            }

            return FirstSprite(name, null);
        }

        static Sprite PotionSprite(string name)
        {
            string path = "Assets/2D Potion Icon Pack/Sprites/" + name + ".png";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path) ?? SpriteNamed(name);
        }

        static Sprite FirstSprite(string name, string folderHint)
        {
            foreach (var guid in AssetDatabase.FindAssets(name + " t:Texture2D"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(folderHint) &&
                    path.IndexOf(folderHint, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
                if (sprites.Length > 0)
                    return sprites[0];
            }

            return null;
        }

        static int IndexOf(Sprite sprite)
        {
            var match = Regex.Match(sprite.name, @"_(\d+)$");
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        static string[] Ids(int from, int to)
        {
            var ids = new string[to - from + 1];
            for (int i = from; i <= to; i++)
                ids[i - from] = "enemy_" + i.ToString("000");
            return ids;
        }
    }
}
