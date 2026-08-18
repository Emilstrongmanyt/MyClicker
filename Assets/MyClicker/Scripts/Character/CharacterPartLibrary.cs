using System;
using System.Collections.Generic;
using MyClicker.Data;
using UnityEngine;

namespace MyClicker.Character
{
    public class CharacterPartLibrary
    {
        public readonly Dictionary<string, List<Sprite>> Slots = new Dictionary<string, List<Sprite>>(StringComparer.OrdinalIgnoreCase);

        public static CharacterPartLibrary Build(GameConfig config)
        {
            var lib = new CharacterPartLibrary();
            string[] slots = config != null && config.character.slotOrder != null && config.character.slotOrder.Length > 0
                ? config.character.slotOrder
                : new[] { "Body", "Head", "Hair", "Eyes", "Armor", "Helmet", "Weapon", "Shield", "Cape" };

            foreach (var slot in slots)
                lib.Slots[slot] = new List<Sprite>();

            if (config?.character.slots != null)
            {
                foreach (var entry in config.character.slots)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.slot) || entry.sprites == null)
                        continue;
                    if (!lib.Slots.TryGetValue(entry.slot, out var list))
                    {
                        list = new List<Sprite>();
                        lib.Slots[entry.slot] = list;
                    }

                    foreach (var sprite in entry.sprites)
                    {
                        if (sprite != null && !list.Contains(sprite))
                            list.Add(sprite);
                    }
                }
            }

            return lib;
        }

        public Sprite Get(string slot, int index)
        {
            if (!Slots.TryGetValue(slot, out var list) || list.Count == 0)
                return null;
            int i = ((index % list.Count) + list.Count) % list.Count;
            return list[i];
        }

        public int Count(string slot) => Slots.TryGetValue(slot, out var list) ? list.Count : 0;
    }
}
