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
            public float spawnInterval = 0.85f;
            public int maxAlive = 6;
            public float enemySpeed = 2.15f;
            public float tapDamage = 10f;
            public float enemyBaseHp = 26f;
            public float enemyHpPerWave = 7.5f;
            public float bossHpMul = 9f;
            public float approachStopDistance = 1.55f;
            public int killsPerWave = 8;
            public int wavesPerBoss = 10;
            public Vector2 spawnX = new Vector2(-4.4f, 4.4f);
            public Vector2 spawnY = new Vector2(5.8f, 7.6f);
            public Vector2 playerSlot = new Vector2(0f, -4.15f);
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
            public int goldPerKill = 5;
            public float goldPerKillPerWave = 1.15f;
            public int goldPerWave = 18;
            public int goldPerBoss = 70;
            public float goldPerBossPerWave = 6f;
            public int dustPerBoss = 3;
            public float dustDropChance = 0.08f;
            public float potionDropChance = 0.08f;
            public float potionBossDropChance = 1f;
            public float mightPerLevel = 4f;
            public float fortunePerLevel = 0.12f;
            public float autoIntervalStart = 2.35f;
            public float autoIntervalDecay = 0.93f;
            public float autoIntervalMin = 0.28f;
            public float critPerLevel = 0.02f;
            public float critChanceCap = 0.6f;
            public float critMultiplier = 3f;
            public float mightPotionBonus = 0.6f;
            public float swiftPotionBonus = 0.35f;
            public float goldPotionBonus = 1f;
            public float offlineCapHours = 8f;
            public float offlineGoldFactor = 0.55f;
        }
    }
}
