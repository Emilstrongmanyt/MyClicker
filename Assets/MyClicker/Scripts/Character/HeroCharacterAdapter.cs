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

        public static readonly string[] GearSlots = { "Weapon", "Armor", "Helmet", "Cape" };

        public int SlotCount(string slot)
        {
            if (slot == "Weapon")
                return WeaponOptions().Count;
            return ItemsFor(slot).Count;
        }

        public int SlotIndex(string slot)
        {
            if (slot == "Weapon")
            {
                var options = WeaponOptions();
                int index = options.FindIndex(IsEquippedWeapon);
                return index < 0 ? 0 : index;
            }

            var items = ItemsFor(slot);
            int found = items.FindIndex(i => IsCurrent(slot, i));
            return found < 0 ? 0 : found;
        }

        public string SlotId(string slot)
        {
            if (slot == "Weapon")
            {
                var options = WeaponOptions();
                int index = SlotIndex(slot);
                if (options.Count == 0 || index < 0 || index >= options.Count)
                    return slot;
                return options[index].Item != null ? options[index].Item.Id : slot;
            }

            var items = ItemsFor(slot);
            int i = SlotIndex(slot);
            if (items.Count == 0 || i < 0 || i >= items.Count || items[i] == null)
                return slot;
            return items[i].Id ?? slot;
        }

        public string SlotLabel(string slot)
        {
            if (slot == "Weapon")
            {
                var options = WeaponOptions();
                int index = SlotIndex(slot);
                if (options.Count == 0 || index < 0 || index >= options.Count)
                    return "None";
                return Pretty(options[index].Item);
            }

            var items = ItemsFor(slot);
            int i = SlotIndex(slot);
            if (items.Count == 0 || i < 0 || i >= items.Count)
                return "None";
            return Pretty(items[i]);
        }

        public void Cycle(string slot, int delta)
        {
            int count = SlotCount(slot);
            if (count <= 0)
                return;
            SetSlotIndex(slot, SlotIndex(slot) + delta);
        }

        public void SetSlotIndex(string slot, int index)
        {
            if (Hero == null || Hero.SpriteCollection == null)
                return;

            if (slot == "Weapon")
            {
                var options = WeaponOptions();
                if (options.Count == 0)
                    return;
                int next = ((index % options.Count) + options.Count) % options.Count;
                var choice = options[next];
                Hero.Equip(choice.Item, choice.Part);
                return;
            }

            var items = ItemsFor(slot);
            if (items.Count == 0)
                return;
            int clamped = ((index % items.Count) + items.Count) % items.Count;
            Apply(slot, items[clamped]);
        }

        void Apply(string slot, ItemSprite item)
        {
            switch (slot)
            {
                case "Hair":
                    Hero.SetBody(item, BodyPart.Hair);
                    break;
                case "Eyes":
                    Hero.SetBody(item, BodyPart.Eyes);
                    break;
                case "Armor":
                    if (item == null) Hero.UnEquip(EquipmentPart.Armor);
                    else Hero.Equip(item, EquipmentPart.Armor);
                    break;
                case "Helmet":
                    if (item == null) Hero.UnEquip(EquipmentPart.Helmet);
                    else Hero.Equip(item, EquipmentPart.Helmet);
                    break;
                case "Cape":
                    if (item == null) Hero.UnEquip(EquipmentPart.Cape);
                    else Hero.Equip(item, EquipmentPart.Cape);
                    break;
            }
        }

        List<ItemSprite> ItemsFor(string slot)
        {
            if (Hero?.SpriteCollection == null)
                return new List<ItemSprite>();
            switch (slot)
            {
                case "Hair": return Preferred(Hero.SpriteCollection.Hair);
                case "Eyes": return Preferred(Hero.SpriteCollection.Eyes);
                case "Armor": return FullArmorSets(Hero.SpriteCollection.Armor);
                case "Helmet": return Preferred(Hero.SpriteCollection.Helmet);
                case "Cape": return Preferred(Hero.SpriteCollection.Cape);
                default: return new List<ItemSprite>();
            }
        }

        bool IsCurrent(string slot, ItemSprite item)
        {
            if (item == null)
                return false;
            switch (slot)
            {
                case "Hair": return Hero.Hair != null && item.Sprite == Hero.Hair;
                case "Eyes": return Hero.EyesRenderer != null && item.Sprite == Hero.EyesRenderer.sprite;
                case "Armor": return SameArmor(item);
                case "Helmet": return Hero.Helmet != null && item.Sprite == Hero.Helmet;
                case "Cape": return Hero.Cape != null && WorldSprite(item) == Hero.Cape;
                default: return false;
            }
        }

        List<WeaponOption> WeaponOptions()
        {
            var options = new List<WeaponOption>();
            if (Hero?.SpriteCollection == null)
                return options;
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

            return options;
        }

        static string Pretty(ItemSprite item)
        {
            if (item == null)
                return "None";
            string id = item.Id ?? "Gear";
            int dot = id.LastIndexOf('.');
            if (dot >= 0 && dot + 1 < id.Length)
                id = id.Substring(dot + 1);
            return id.Replace('_', ' ');
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
    }
}
