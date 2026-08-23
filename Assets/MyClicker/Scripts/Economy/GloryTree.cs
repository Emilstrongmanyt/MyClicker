using MyClicker.App;

namespace MyClicker.Economy
{
    public static class GloryIds
    {
        public const string Legacy = "legacy";
        public const string KeepMight = "keep_might";
        public const string UnspentTithe = "unspent_tithe";
        public const string FocusWell = "focus_well";
        public const string Steel = "steel";
        public const string KeepFortune = "keep_fortune";
        public const string Hoard = "hoard";
        public const string DeedAngel = "deed_angel";
        public const string DeepRoad = "deep_road";
        public const string NightMarket = "night_market";
        public const string BloodOath = "blood_oath";
        public const string GiantsDue = "giants_due";
        public const string CrushingSlam = "crushing_slam";
        public const string EternalFury = "eternal_fury";
        public const string WideSweep = "wide_sweep";
        public const string IronPulse = "iron_pulse";
        public const string TitanHeart = "titan_heart";
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
        public int treeRow;
        public int treeCol;
    }

    public static class GloryTree
    {
        public static readonly GloryNode[] All =
        {
            Node(GloryIds.Legacy, "Legacy", "Opens the Glory talent tree.", 0, null, 0, -1, 1, 0),
            Node(GloryIds.KeepMight, "Keep Might", "Might ranks survive ascend.", 40, GloryIds.Legacy, 1, 0, 0, 0),
            Node(GloryIds.FocusWell, "Focus Well", "+25% Focus max and +50% regen.", 50, GloryIds.Legacy, 1, 1, 0, 0),
            Node(GloryIds.KeepFortune, "Keep Fortune", "Fortune ranks survive ascend.", 120, GloryIds.KeepMight, 2, 0, 0, 0),
            Node(GloryIds.UnspentTithe, "Unspent Tithe", "Unspent Glory raises run gold (capped).", 35, GloryIds.Legacy, 2, 1, 0, 0),
            Node(GloryIds.Steel, "Steel", "Temper ranks add tap.", 90, GloryIds.KeepMight, 3, 0, 0, 0),
            Node(GloryIds.Hoard, "Hoard", "Each relic adds gold.", 90, GloryIds.Legacy, 3, 1, 0, 0),
            Node(GloryIds.NightMarket, "Night Market", "Away gold is a bit better.", 45, GloryIds.UnspentTithe, 4, 0, 0, 0),
            Node(GloryIds.DeedAngel, "Deed Angel", "Renown from Deeds is 50% stronger.", 180, GloryIds.Legacy, 4, 1, 0, 0),
            Node(GloryIds.DeepRoad, "Deep Road", "Endless cycles pay extra.", 220, GloryIds.Legacy, 5, 0, 0, 9),
            Node(GloryIds.CrushingSlam, "Crushing Slam", "Slam hits much harder.", 300, GloryIds.FocusWell, 5, 1, 0, 0),
            Node(GloryIds.EternalFury, "Eternal Fury", "Fury lasts longer and hits harder.", 400, GloryIds.FocusWell, 6, 0, 0, 0),
            Node(GloryIds.WideSweep, "Wide Sweep", "Sweep hits much harder.", 350, GloryIds.FocusWell, 6, 1, 0, 0),
            Node(GloryIds.BloodOath, "Blood Oath", "+25% tap and auto.", 200, GloryIds.KeepMight, 7, 0, 0, 0),
            Node(GloryIds.GiantsDue, "Giant's Due", "+50% tap and auto.", 600, GloryIds.BloodOath, 7, 1, 0, 0),
            Node(GloryIds.IronPulse, "Iron Pulse", "Permanent x2 tap and auto.", 2500, GloryIds.KeepMight, 8, -1, 3, 0),
            Node(GloryIds.TitanHeart, "Titan Heart", "Another permanent x2 tap and auto.", 8000, GloryIds.IronPulse, 9, -1, 5, 0),
        };

        static GloryNode Node(string id, string title, string blurb, int cost, string requiresId, int row, int col, int requiresAscend, int requiresBestZone)
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
                requiresBestZone = requiresBestZone
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

        public static bool Has(PlayerProfile profile, string id)
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
            if (profile == null || string.IsNullOrEmpty(id) || Has(profile, id))
                return false;
            int n = profile.gloryNodes != null ? profile.gloryNodes.Length : 0;
            var next = new string[n + 1];
            if (n > 0)
                System.Array.Copy(profile.gloryNodes, next, n);
            next[n] = id;
            profile.gloryNodes = next;
            return true;
        }

        public static bool CanBuy(PlayerProfile profile, GloryNode node)
        {
            if (profile == null || node == null || Has(profile, node.id))
                return false;
            if (node.requiresAscend > 0 && profile.ascendCount < node.requiresAscend)
                return false;
            if (node.requiresBestZone > 0 && profile.bestZone < node.requiresBestZone)
                return false;
            if (!string.IsNullOrEmpty(node.requiresId) && !Has(profile, node.requiresId))
                return false;
            return profile.glory >= node.cost;
        }

        public static string LockReason(PlayerProfile profile, GloryNode node)
        {
            if (node == null)
                return "";
            if (Has(profile, node.id))
                return "Owned";
            if (node.requiresAscend > 0 && profile.ascendCount < node.requiresAscend)
                return "Ascend " + node.requiresAscend;
            if (!string.IsNullOrEmpty(node.requiresId) && !Has(profile, node.requiresId))
                return "Needs " + Title(node.requiresId);
            if (node.requiresBestZone > 0 && profile.bestZone < node.requiresBestZone)
                return "Reach Harvest Night";
            if (profile.glory < node.cost)
                return "Need " + node.cost;
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
    }
}
