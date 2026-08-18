using UnityEngine;

namespace MyClicker.Data
{
    [CreateAssetMenu(menuName = "MyClicker/Game Config", fileName = "GameConfig")]
    public class GameConfig : ScriptableObject
    {
        public UiSkin ui = new UiSkin();
        public CharacterCatalog character = new CharacterCatalog();
        public CombatSettings combat = new CombatSettings();
        public WorldSettings world = new WorldSettings();
        public EconomySettings economy = new EconomySettings();

        [System.Serializable]
        public class UiSkin
        {
            public Sprite panel;
            public Sprite buttonNormal;
            public Sprite buttonPressed;
            public Sprite buttonDisabled;
            public Sprite hpFill;
            public Sprite hpBackground;
            public Sprite coinIcon;
            public Sprite bannerReady;
            public Sprite bannerVictory;
            public Sprite bannerLevelUp;
            public Color textColor = new Color(0.95f, 0.90f, 0.80f);
            public Color shadowColor = new Color(0.15f, 0.10f, 0.07f, 0.85f);
        }

        [System.Serializable]
        public class SlotSprites
        {
            public string slot;
            public Sprite[] sprites;
        }

        [System.Serializable]
        public class CharacterCatalog
        {
            public GameObject heroPrefab;
            public string[] partRoots;
            public string[] slotOrder = { "Body", "Head", "Hair", "Eyes", "Armor", "Helmet", "Weapon", "Shield", "Cape" };
            public SlotSprites[] slots;
        }

        [System.Serializable]
        public class CombatSettings
        {
            public Sprite[] enemySprites;
            public GameObject[] enemyPrefabs;
            public float spawnInterval = 1.4f;
            public int maxAlive = 8;
            public float enemySpeed = 1.6f;
            public float tapDamage = 12f;
            public float enemyBaseHp = 30f;
            public float enemyHpPerWave = 8f;
            public float approachStopDistance = 1.4f;
            public Vector2 spawnX = new Vector2(-4.2f, 4.2f);
            public Vector2 spawnY = new Vector2(6.5f, 8.2f);
            public Vector2 playerSlot = new Vector2(0f, -4.4f);
        }

        [System.Serializable]
        public class WorldSettings
        {
            public Sprite[] backgroundSprites;
        }

        [System.Serializable]
        public class EconomySettings
        {
            public Sprite[] potionIcons;
            public int startingGold = 0;
            public int goldPerKill = 3;
            public int goldPerWave = 12;
            public float potionDropChance = 0.12f;
        }
    }
}
