using System;
using UnityEngine;

namespace MyClicker.App
{
    [Serializable]
    public class PlayerProfile
    {
        public string displayName = "Hero";
        public bool hasCharacter;
        public long gold;
        public int dust;
        public int glory;
        public int zone;
        public int wave = 1;
        public int kills;
        public int bossesSlain;
        public int runBosses;
        public int pendingGlory;
        public float tapDamage = 12f;
        public int mightLevel;
        public int fortuneLevel;
        public int swiftLevel;
        public int critLevel;
        public int cleaveLevel;
        public int furyLevel;
        public int harvestLevel;
        public int mutationMight;
        public int mutationFortune;
        public int mutationSwift;
        public int mutationLuck;
        public int ascendCount;
        public float focus;
        public int temperWeapon;
        public int temperArmor;
        public int temperHelmet;
        public int temperCape;
        public int potMight;
        public int potSwift;
        public int potGold;
        public float mightBuffLeft;
        public float swiftBuffLeft;
        public float goldBuffLeft;
        public bool seenTapHint;
        public bool seenArmoryHint;
        public long lastSeenUnix;
        public string heroJson;
        public string[] unlockedGear = new string[0];
        public CharacterSave character = new CharacterSave();

        public bool HasGear(string id)
        {
            if (string.IsNullOrEmpty(id) || unlockedGear == null)
                return false;
            for (int i = 0; i < unlockedGear.Length; i++)
            {
                if (unlockedGear[i] == id)
                    return true;
            }

            return false;
        }

        public bool UnlockGear(string id)
        {
            if (string.IsNullOrEmpty(id) || HasGear(id))
                return false;
            int n = unlockedGear != null ? unlockedGear.Length : 0;
            var next = new string[n + 1];
            if (n > 0)
                Array.Copy(unlockedGear, next, n);
            next[n] = id;
            unlockedGear = next;
            return true;
        }

        public int UpgradeLevel(string id)
        {
            switch (id)
            {
                case Data.ContentIds.Might: return mightLevel;
                case Data.ContentIds.Fortune: return fortuneLevel;
                case Data.ContentIds.Swift: return swiftLevel;
                case Data.ContentIds.Crit: return critLevel;
                case Data.ContentIds.Cleave: return cleaveLevel;
                case Data.ContentIds.Fury: return furyLevel;
                case Data.ContentIds.Harvest: return harvestLevel;
                default: return 0;
            }
        }

        public void SetUpgradeLevel(string id, int value)
        {
            value = Mathf.Max(0, value);
            switch (id)
            {
                case Data.ContentIds.Might: mightLevel = value; break;
                case Data.ContentIds.Fortune: fortuneLevel = value; break;
                case Data.ContentIds.Swift: swiftLevel = value; break;
                case Data.ContentIds.Crit: critLevel = value; break;
                case Data.ContentIds.Cleave: cleaveLevel = value; break;
                case Data.ContentIds.Fury: furyLevel = value; break;
                case Data.ContentIds.Harvest: harvestLevel = value; break;
            }
        }

        public int TemperLevel(string slot)
        {
            switch (slot)
            {
                case "Weapon": return temperWeapon;
                case "Armor": return temperArmor;
                case "Helmet": return temperHelmet;
                case "Cape": return temperCape;
                default: return 0;
            }
        }

        public void SetTemperLevel(string slot, int value)
        {
            value = Mathf.Max(0, value);
            switch (slot)
            {
                case "Weapon": temperWeapon = value; break;
                case "Armor": temperArmor = value; break;
                case "Helmet": temperHelmet = value; break;
                case "Cape": temperCape = value; break;
            }
        }

        public int PotionCount(string id)
        {
            switch (id)
            {
                case Data.ContentIds.PotMight: return potMight;
                case Data.ContentIds.PotSwift: return potSwift;
                case Data.ContentIds.PotGold: return potGold;
                default: return 0;
            }
        }

        public int MutationLevel(string id)
        {
            switch (id)
            {
                case Data.ContentIds.MutMight: return mutationMight;
                case Data.ContentIds.MutFortune: return mutationFortune;
                case Data.ContentIds.MutSwift: return mutationSwift;
                case Data.ContentIds.MutLuck: return mutationLuck;
                default: return 0;
            }
        }

        public void SetMutationLevel(string id, int value)
        {
            value = Mathf.Max(0, value);
            switch (id)
            {
                case Data.ContentIds.MutMight: mutationMight = value; break;
                case Data.ContentIds.MutFortune: mutationFortune = value; break;
                case Data.ContentIds.MutSwift: mutationSwift = value; break;
                case Data.ContentIds.MutLuck: mutationLuck = value; break;
            }
        }

        public void SetPotionCount(string id, int value)
        {
            value = Mathf.Max(0, value);
            switch (id)
            {
                case Data.ContentIds.PotMight: potMight = value; break;
                case Data.ContentIds.PotSwift: potSwift = value; break;
                case Data.ContentIds.PotGold: potGold = value; break;
            }
        }
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
