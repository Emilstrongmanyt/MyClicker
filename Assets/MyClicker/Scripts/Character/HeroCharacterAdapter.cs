using System;
using System.Collections.Generic;
using System.Linq;
using HeroEditor.Common.Data;
using HeroEditor.Common.Enums;
using UnityEngine;
using HeroCharacter = Assets.HeroEditor.Common.Scripts.CharacterScripts.Character;

namespace MyClicker.Character
{
    public class HeroCharacterAdapter : MonoBehaviour
    {
        public HeroCharacter Hero { get; private set; }

        static readonly string[] CycleSlots = { "Hair", "Eyes", "Armor", "Helmet", "Weapon", "Cape" };

        struct WeaponOption
        {
            public ItemSprite Item;
            public EquipmentPart Part;
        }

        public static HeroCharacterAdapter Spawn(GameObject prefab, string json, Vector3 position, float scale)
        {
            if (prefab == null)
            {
                Debug.LogError("[MyClicker] Cannot spawn hero: prefab is null.");
                return null;
            }

            var root = new GameObject("Hero");
            root.transform.position = position;
            root.transform.localScale = Vector3.one * scale;
            var adapter = root.AddComponent<HeroCharacterAdapter>();
            var instance = UnityEngine.Object.Instantiate(prefab, root.transform, false);
            instance.name = "Human";
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            adapter.Hero = instance.GetComponent<HeroCharacter>() ?? instance.GetComponentInChildren<HeroCharacter>(true);
            if (adapter.Hero == null)
            {
                Debug.LogError("[MyClicker] Instantiated prefab has no HeroEditor Character component.");
                return adapter;
            }

            if (!string.IsNullOrEmpty(json))
            {
                try { adapter.Hero.FromJson(json); }
                catch (Exception ex) { Debug.LogWarning("[MyClicker] Hero FromJson failed: " + ex.Message); }
            }

            adapter.Hero.Initialize();
            return adapter;
        }

        public string ToJson()
        {
            return Hero != null ? Hero.ToJson() : "";
        }

        public IReadOnlyList<string> Slots => CycleSlots;

        public void Cycle(string slot, int delta)
        {
            if (Hero == null || Hero.SpriteCollection == null)
                return;

            switch (slot)
            {
                case "Hair":
                    CycleItems(Preferred(Hero.SpriteCollection.Hair),
                        item => Hero.Hair != null && item.Sprite == Hero.Hair,
                        item => Hero.SetBody(item, BodyPart.Hair),
                        delta, allowEmpty: true);
                    break;
                case "Eyes":
                    CycleItems(Preferred(Hero.SpriteCollection.Eyes),
                        item => Hero.EyesRenderer != null && item.Sprite == Hero.EyesRenderer.sprite,
                        item => Hero.SetBody(item, BodyPart.Eyes),
                        delta);
                    break;
                case "Armor":
                    CycleItems(FullArmorSets(Hero.SpriteCollection.Armor),
                        SameArmor,
                        item =>
                        {
                            if (item == null) Hero.UnEquip(EquipmentPart.Armor);
                            else Hero.Equip(item, EquipmentPart.Armor);
                        },
                        delta, allowEmpty: true);
                    break;
                case "Helmet":
                    CycleItems(Preferred(Hero.SpriteCollection.Helmet),
                        item => Hero.Helmet != null && item.Sprite == Hero.Helmet,
                        item =>
                        {
                            if (item == null) Hero.UnEquip(EquipmentPart.Helmet);
                            else Hero.Equip(item, EquipmentPart.Helmet);
                        },
                        delta, allowEmpty: true);
                    break;
                case "Weapon":
                    CycleWeapons(delta);
                    break;
                case "Cape":
                    CycleItems(Preferred(Hero.SpriteCollection.Cape),
                        item => Hero.Cape != null && WorldSprite(item) == Hero.Cape,
                        item =>
                        {
                            if (item == null) Hero.UnEquip(EquipmentPart.Cape);
                            else Hero.Equip(item, EquipmentPart.Cape);
                        },
                        delta, allowEmpty: true);
                    break;
            }
        }

        void CycleWeapons(int delta)
        {
            var options = new List<WeaponOption>();
            foreach (var item in Preferred(Hero.SpriteCollection.MeleeWeapon1H))
            {
                if (WorldSprite(item) != null)
                    options.Add(new WeaponOption { Item = item, Part = EquipmentPart.MeleeWeapon1H });
            }

            foreach (var item in Preferred(Hero.SpriteCollection.MeleeWeapon2H))
            {
                if (WorldSprite(item) != null)
                    options.Add(new WeaponOption { Item = item, Part = EquipmentPart.MeleeWeapon2H });
            }

            foreach (var item in Preferred(Hero.SpriteCollection.Bow))
            {
                if (item.Sprites != null && item.Sprites.Count > 0)
                    options.Add(new WeaponOption { Item = item, Part = EquipmentPart.Bow });
            }

            if (options.Count == 0)
                return;

            int index = options.FindIndex(o => IsEquippedWeapon(o));
            if (index < 0)
                index = 0;
            int next = ((index + delta) % options.Count + options.Count) % options.Count;
            var choice = options[next];
            Hero.Equip(choice.Item, choice.Part);
        }

        bool IsEquippedWeapon(WeaponOption option)
        {
            switch (option.Part)
            {
                case EquipmentPart.MeleeWeapon1H:
                    return Hero.WeaponType == WeaponType.Melee1H && WorldSprite(option.Item) == Hero.PrimaryMeleeWeapon;
                case EquipmentPart.MeleeWeapon2H:
                    return Hero.WeaponType == WeaponType.Melee2H && WorldSprite(option.Item) == Hero.PrimaryMeleeWeapon;
                case EquipmentPart.Bow:
                    return Hero.WeaponType == WeaponType.Bow && option.Item.Sprites != null && Hero.Bow != null
                           && option.Item.Sprites.Intersect(Hero.Bow).Any();
                default:
                    return false;
            }
        }

        bool SameArmor(ItemSprite item)
        {
            if (item == null || Hero.Armor == null || item.Sprites == null)
                return false;
            var torso = item.Sprites.FirstOrDefault(s => s != null && s.name == "Torso");
            return torso != null && Hero.Armor.Contains(torso);
        }

        static List<ItemSprite> FullArmorSets(List<ItemSprite> source)
        {
            return Preferred(source).Where(IsFullArmor).ToList();
        }

        static bool IsFullArmor(ItemSprite item)
        {
            if (item?.Sprites == null || item.Sprites.Count < 3)
                return false;
            var names = new HashSet<string>(item.Sprites.Where(s => s != null).Select(s => s.name));
            return names.Contains("Torso") && (names.Contains("Pelvis") || names.Contains("ArmL") || names.Contains("ArmR"));
        }

        static List<ItemSprite> Preferred(List<ItemSprite> source)
        {
            if (source == null)
                return new List<ItemSprite>();

            var usable = source.Where(i => i != null && !IsSeasonal(i)).ToList();
            var basic = usable.Where(IsBasic).ToList();
            return basic.Count > 0 ? basic : usable;
        }

        static bool IsBasic(ItemSprite item)
        {
            string id = item.Id ?? "";
            string collection = item.Collection ?? "";
            return collection.IndexOf("Basic", StringComparison.OrdinalIgnoreCase) >= 0
                   || id.IndexOf(".Basic.", StringComparison.OrdinalIgnoreCase) >= 0
                   || (string.IsNullOrEmpty(collection) && !IsSeasonal(item));
        }

        static bool IsSeasonal(ItemSprite item)
        {
            string hay = ((item.Id ?? "") + " " + (item.Collection ?? "")).ToLowerInvariant();
            return hay.Contains("christmas") || hay.Contains("chrismas") || hay.Contains("halloween");
        }

        static Sprite WorldSprite(ItemSprite item)
        {
            if (item == null)
                return null;
            if (item.Sprite != null)
                return item.Sprite;
            return item.Sprites?.FirstOrDefault(s => s != null);
        }

        static void CycleItems(List<ItemSprite> items, Func<ItemSprite, bool> isCurrent, Action<ItemSprite> apply, int delta, bool allowEmpty = false)
        {
            if (items == null || items.Count == 0)
                return;

            int index = items.FindIndex(i => isCurrent(i));
            int count = items.Count + (allowEmpty ? 1 : 0);
            if (index < 0)
                index = 0;
            index = ((index + delta) % count + count) % count;
            if (allowEmpty && index == items.Count)
            {
                apply(null);
                return;
            }

            apply(items[index]);
        }
    }
}
