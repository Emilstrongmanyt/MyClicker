using MyClicker.App;

namespace MyClicker.Economy
{
    public static class GloryIds
    {
        public const string Legacy = "legacy";
        public const string KeepMight = "keep_might";
        public const string KeepFortune = "keep_fortune";
        public const string KeepSwift = "keep_swift";
        public const string KeepRoad = "keep_road";
        public const string UnspentTithe = "unspent_tithe";
        public const string FocusWell = "focus_well";
        public const string Steel = "steel";
        public const string Hoard = "hoard";
        public const string DeedAngel = "deed_angel";
        public const string DeepRoad = "deep_road";
        public const string NightMarket = "night_market";
        public const string BloodOath = "blood_oath";
        public const string GiantsDue = "giants_due";
        public const string CrushingSlam = "crushing_slam";
        public const string EternalFury = "eternal_fury";
        public const string WideSweep = "wide_sweep";
        public const string ReaperSweep = "reaper_sweep";
        public const string IronPulse = "iron_pulse";
        public const string TitanHeart = "titan_heart";
        public const string WarTempo = "war_tempo";
        public const string ForcedMarch = "forced_march";
        public const string GoldVein = "gold_vein";
        public const string LuckyStrike = "lucky_strike";
        public const string SecondWind = "second_wind";
        public const string RelicSense = "relic_sense";
        public const string BossTithe = "boss_tithe";
        public const string SwiftEcho = "swift_echo";
        public const string HordeBanner = "horde_banner";
        public const string Mythos = "mythos";
    }

    public static class GloryPaths
    {
        public const string Keep = "Keep";
        public const string Focus = "Focus";
        public const string Horde = "Horde";
        public const string Gold = "Gold";
        public const string Power = "Power";
        public static readonly string[] All = { Keep, Focus, Horde, Gold, Power };
    }

    [System.Serializable]
    public class GloryNode
    {
        public string id;
        public string title;
        public string blurb;
        public int cost;
        public string requiresId;
        public int requiresAscend;
        public int requiresBestZone;
        public int requiresBestCycle;
        public int treeRow;
        public int treeCol;
        public int maxRank = 1;
        public float costGrowth = 1.55f;
        public string path;
    }

    public static class GloryTree
    {
        public static readonly GloryNode[] All =
        {
            Once(GloryIds.Legacy, "Legacy", "Opens the Glory talent tree. Stays after Respec.", 0, null, 0, -1, 0, 0, GloryPaths.Keep),
            Once(GloryIds.KeepMight, "Keep Might", "Might ranks survive ascend.", 40, GloryIds.Legacy, 1, 0, 0, 0, GloryPaths.Keep),
            Once(GloryIds.KeepFortune, "Keep Fortune", "Fortune ranks survive ascend.", 120, GloryIds.KeepMight, 2, 0, 2, 0, GloryPaths.Keep),
            Once(GloryIds.KeepSwift, "Keep Swift", "Swift ranks survive ascend.", 150, GloryIds.KeepMight, 3, 0, 2, 0, GloryPaths.Keep),
            Once(GloryIds.KeepRoad, "Keep Road", "Ascend on Endless stays on that cycle. After a loop, new runs start in Endless 1.", 280, GloryIds.DeepRoad, 4, 0, 0, 0, GloryPaths.Keep, 1),
            Once(GloryIds.Steel, "Steel", "Temper ranks add tap.", 90, GloryIds.KeepMight, 5, 0, 0, 0, GloryPaths.Keep),
            Ranked(GloryIds.BloodOath, "Blood Oath", "+5% tap and auto each rank.", 80, GloryIds.KeepMight, 6, 0, 12, 1.45f, GloryPaths.Keep),
            Once(GloryIds.SwiftEcho, "Swift Echo", "Overclock auto damage is stronger.", 900, GloryIds.KeepMight, 7, 0, 3, 0, GloryPaths.Keep),

            Ranked(GloryIds.FocusWell, "Focus Well", "+8% Focus max and +12% regen per rank.", 50, GloryIds.Legacy, 1, 1, 8, 1.5f, GloryPaths.Focus),
            Ranked(GloryIds.CrushingSlam, "Crushing Slam", "Slam hits harder each rank.", 80, GloryIds.FocusWell, 2, 1, 8, 1.5f, GloryPaths.Focus),
            Ranked(GloryIds.EternalFury, "Eternal Fury", "Fury lasts longer and hits harder each rank.", 90, GloryIds.FocusWell, 3, 1, 8, 1.5f, GloryPaths.Focus),
            Ranked(GloryIds.WideSweep, "Wide Sweep", "Sweep hits harder each rank.", 85, GloryIds.FocusWell, 4, 1, 8, 1.5f, GloryPaths.Focus),
            Once(GloryIds.ReaperSweep, "Reaper Sweep", "Sweep becomes a single heavy strike that hits bosses. Respec to return to horde Sweep.", 220, GloryIds.WideSweep, 5, 1, 0, 0, GloryPaths.Focus),
            Once(GloryIds.SecondWind, "Second Wind", "Slam, Fury, and Sweep cost 20% less Focus.", 700, GloryIds.FocusWell, 6, 1, 2, 0, GloryPaths.Focus),

            Ranked(GloryIds.WarTempo, "War Tempo", "Enemies spawn faster each rank.", 70, GloryIds.Legacy, 1, 0, 10, 1.5f, GloryPaths.Horde),
            Ranked(GloryIds.ForcedMarch, "Forced March", "Enemies walk in faster each rank.", 110, GloryIds.WarTempo, 2, 0, 8, 1.55f, GloryPaths.Horde),
            Once(GloryIds.HordeBanner, "Horde Banner", "More invaders can be on the field.", 500, GloryIds.WarTempo, 3, 0, 0, 0, GloryPaths.Horde),

            Ranked(GloryIds.UnspentTithe, "Unspent Tithe", "Banked Glory raises gold. Stronger each rank, diminishing.", 35, GloryIds.Legacy, 1, 1, 8, 1.5f, GloryPaths.Gold),
            Once(GloryIds.Hoard, "Hoard", "Each relic adds gold.", 90, GloryIds.Legacy, 2, 1, 0, 0, GloryPaths.Gold),
            Ranked(GloryIds.NightMarket, "Night Market", "Away gold improves each rank.", 45, GloryIds.UnspentTithe, 3, 1, 6, 1.5f, GloryPaths.Gold),
            Ranked(GloryIds.DeedAngel, "Deed Angel", "Renown from Deeds grows each rank.", 90, GloryIds.Legacy, 4, 1, 8, 1.5f, GloryPaths.Gold),
            Ranked(GloryIds.GoldVein, "Gold Vein", "+5% gold each rank.", 100, GloryIds.Hoard, 5, 1, 10, 1.5f, GloryPaths.Gold),
            Once(GloryIds.RelicSense, "Relic Sense", "More dust, potions, and relic drops.", 600, GloryIds.Hoard, 6, 1, 0, 0, GloryPaths.Gold),
            Ranked(GloryIds.BossTithe, "Boss Tithe", "Bosses bank more Glory each rank.", 150, GloryIds.DeepRoad, 7, 1, 8, 1.55f, GloryPaths.Gold),

            Once(GloryIds.DeepRoad, "Deep Road", "Endless cycles pay extra.", 220, GloryIds.Legacy, 1, 0, 0, 9, GloryPaths.Power),
            Once(GloryIds.LuckyStrike, "Lucky Strike", "+0.55 crit multiplier.", 800, GloryIds.BloodOath, 2, 0, 2, 0, GloryPaths.Power),
            Once(GloryIds.GiantsDue, "Giant's Due", "+25% tap and auto.", 600, GloryIds.BloodOath, 3, 0, 0, 0, GloryPaths.Power),
            Once(GloryIds.IronPulse, "Iron Pulse", "Permanent +50% tap and auto.", 2500, GloryIds.KeepMight, 4, -1, 3, 0, GloryPaths.Power),
            Once(GloryIds.Mythos, "Mythos", "+15% tap and auto.", 4000, GloryIds.IronPulse, 5, 1, 4, 0, GloryPaths.Power),
            Once(GloryIds.TitanHeart, "Titan Heart", "Another permanent +50% tap and auto.", 8000, GloryIds.IronPulse, 6, -1, 5, 0, GloryPaths.Power),
        };

        static GloryNode Once(string id, string title, string blurb, int cost, string requiresId, int row, int col, int requiresAscend, int requiresBestZone, string path, int requiresBestCycle = 0)
        {
            return new GloryNode
            {
                id = id,
                title = title,
                blurb = blurb,
                cost = cost,
                requiresId = requiresId,
                treeRow = row,
                treeCol = col,
                requiresAscend = requiresAscend,
                requiresBestZone = requiresBestZone,
                requiresBestCycle = requiresBestCycle,
                path = path,
                maxRank = 1,
                costGrowth = 1.55f
            };
        }

        static GloryNode Ranked(string id, string title, string blurb, int cost, string requiresId, int row, int col, int maxRank, float growth, string path)
        {
            return new GloryNode
            {
                id = id,
                title = title,
                blurb = blurb,
                cost = cost,
                requiresId = requiresId,
                treeRow = row,
                treeCol = col,
                maxRank = maxRank,
                costGrowth = growth,
                path = path
            };
        }

        public static int MaxRow
        {
            get
            {
                int max = 0;
                for (int i = 0; i < All.Length; i++)
                {
                    if (All[i].treeRow > max)
                        max = All[i].treeRow;
                }

                return max;
            }
        }

        public static GloryNode Find(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].id == id)
                    return All[i];
            }

            return null;
        }

        public static int Rank(PlayerProfile profile, string id)
        {
            if (profile == null || string.IsNullOrEmpty(id))
                return 0;
            if (profile.gloryRanks != null)
            {
                for (int i = 0; i < profile.gloryRanks.Length; i++)
                {
                    var entry = profile.gloryRanks[i];
                    if (entry != null && entry.id == id)
                        return MathfMax(0, entry.rank);
                }
            }

            return HasNode(profile, id) ? 1 : 0;
        }

        public static int RankCost(GloryNode node, int rank)
        {
            if (node == null || rank < 0)
                return 0;
            float growth = node.costGrowth >= 1.05f ? node.costGrowth : 1.55f;
            return UnityEngine.Mathf.Max(0, UnityEngine.Mathf.RoundToInt(node.cost * (float)System.Math.Pow(growth, rank)));
        }

        public static int NextCost(PlayerProfile profile, GloryNode node)
        {
            if (node == null)
                return 0;
            int cost = RankCost(node, Rank(profile, node.id));
            return UnityEngine.Mathf.Max(node.cost > 0 ? 1 : 0, cost);
        }

        public static int Spent(PlayerProfile profile)
        {
            if (profile == null)
                return 0;
            int total = 0;
            for (int i = 0; i < All.Length; i++)
            {
                var node = All[i];
                if (node == null || node.id == GloryIds.Legacy)
                    continue;
                int rank = Rank(profile, node.id);
                for (int r = 0; r < rank; r++)
                    total += RankCost(node, r);
            }

            return total;
        }

        public static int Respec(PlayerProfile profile)
        {
            if (profile == null)
                return 0;
            int spent = Spent(profile);
            profile.gloryRanks = new GloryRank[0];
            profile.gloryNodes = new[] { GloryIds.Legacy };
            return spent;
        }

        public static bool Has(PlayerProfile profile, string id) => Rank(profile, id) > 0;

        static bool HasNode(PlayerProfile profile, string id)
        {
            if (profile == null || string.IsNullOrEmpty(id) || profile.gloryNodes == null)
                return false;
            for (int i = 0; i < profile.gloryNodes.Length; i++)
            {
                if (profile.gloryNodes[i] == id)
                    return true;
            }

            return false;
        }

        public static bool Unlock(PlayerProfile profile, string id)
        {
            if (profile == null || string.IsNullOrEmpty(id) || HasNode(profile, id))
                return false;
            int n = profile.gloryNodes != null ? profile.gloryNodes.Length : 0;
            var next = new string[n + 1];
            if (n > 0)
                System.Array.Copy(profile.gloryNodes, next, n);
            next[n] = id;
            profile.gloryNodes = next;
            return true;
        }

        public static bool AddRank(PlayerProfile profile, string id)
        {
            if (profile == null || string.IsNullOrEmpty(id))
                return false;
            int n = profile.gloryRanks != null ? profile.gloryRanks.Length : 0;
            for (int i = 0; i < n; i++)
            {
                if (profile.gloryRanks[i] != null && profile.gloryRanks[i].id == id)
                {
                    profile.gloryRanks[i].rank++;
                    return true;
                }
            }

            var next = new GloryRank[n + 1];
            if (n > 0)
                System.Array.Copy(profile.gloryRanks, next, n);
            next[n] = new GloryRank { id = id, rank = 1 };
            profile.gloryRanks = next;
            return true;
        }

        public static bool CanBuy(PlayerProfile profile, GloryNode node)
        {
            if (profile == null || node == null)
                return false;
            int rank = Rank(profile, node.id);
            int cap = node.maxRank > 0 ? node.maxRank : 1;
            if (rank >= cap)
                return false;
            if (rank == 0)
            {
                if (node.requiresAscend > 0 && profile.ascendCount < node.requiresAscend)
                    return false;
                if (node.requiresBestZone > 0 && profile.bestZone < node.requiresBestZone)
                    return false;
                if (node.requiresBestCycle > 0 && profile.bestCycle < node.requiresBestCycle)
                    return false;
                if (!string.IsNullOrEmpty(node.requiresId) && !Has(profile, node.requiresId))
                    return false;
            }

            return profile.glory >= NextCost(profile, node);
        }

        public static string LockReason(PlayerProfile profile, GloryNode node)
        {
            if (node == null)
                return "";
            int rank = Rank(profile, node.id);
            int cap = node.maxRank > 0 ? node.maxRank : 1;
            if (rank >= cap)
                return cap > 1 ? "MAX" : "Owned";
            if (rank == 0)
            {
                if (node.requiresAscend > 0 && profile.ascendCount < node.requiresAscend)
                    return "Ascend " + node.requiresAscend;
                if (!string.IsNullOrEmpty(node.requiresId) && !Has(profile, node.requiresId))
                    return "Needs " + Title(node.requiresId);
                if (node.requiresBestZone > 0 && profile.bestZone < node.requiresBestZone)
                    return "Reach Harvest Night";
                if (node.requiresBestCycle > 0 && profile.bestCycle < node.requiresBestCycle)
                    return "Clear a cycle";
            }

            int cost = NextCost(profile, node);
            if (profile.glory < cost)
                return "Need " + cost;
            return "";
        }

        public static string Title(string id)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].id == id)
                    return All[i].title;
            }

            return id;
        }

        static int MathfMax(int a, int b)
        {
            return a > b ? a : b;
        }
    }
}
