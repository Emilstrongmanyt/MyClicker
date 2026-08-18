using System;
using UnityEngine;

namespace MyClicker.App
{
    [Serializable]
    public class PlayerProfile
    {
        public string displayName = "Hero";
        public bool hasCharacter;
        public int gold;
        public int wave = 1;
        public int kills;
        public float tapDamage = 12f;
        public string heroJson;
        public CharacterSave character = new CharacterSave();
    }

    [Serializable]
    public class CharacterSave
    {
        public int body;
        public int head;
        public int hair;
        public int eyes;
        public int armor;
        public int helmet;
        public int weapon;
        public int shield;
        public int cape;
        public Color hairColor = new Color(0.35f, 0.18f, 0.08f);
        public Color armorColor = Color.white;

        public int GetSlot(string slot)
        {
            switch (slot)
            {
                case "Body": return body;
                case "Head": return head;
                case "Hair": return hair;
                case "Eyes": return eyes;
                case "Armor": return armor;
                case "Helmet": return helmet;
                case "Weapon": return weapon;
                case "Shield": return shield;
                case "Cape": return cape;
                default: return 0;
            }
        }

        public void SetSlot(string slot, int value)
        {
            switch (slot)
            {
                case "Body": body = value; break;
                case "Head": head = value; break;
                case "Hair": hair = value; break;
                case "Eyes": eyes = value; break;
                case "Armor": armor = value; break;
                case "Helmet": helmet = value; break;
                case "Weapon": weapon = value; break;
                case "Shield": shield = value; break;
                case "Cape": cape = value; break;
            }
        }
    }
}
