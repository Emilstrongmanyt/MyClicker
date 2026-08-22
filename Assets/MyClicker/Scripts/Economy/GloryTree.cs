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
    }

    public static class GloryTree
    {
        public static readonly GloryNode[] All =
        {
            new GloryNode
            {
                id = GloryIds.Legacy,
                title = "Legacy",
                blurb = "First ascend opens the Glory tree. Relics and Deeds already stay.",
                cost = 0,
                requiresAscend = 1
            },
            new GloryNode
            {
                id = GloryIds.KeepMight,
                title = "Keep Might",
                blurb = "Might ranks survive the next ascend.",
                cost = 8,
                requiresId = GloryIds.Legacy
            },
            new GloryNode
            {
                id = GloryIds.UnspentTithe,
                title = "Unspent Tithe",
                blurb = "Unspent Glory also raises gold this run (small, capped).",
                cost = 5,
                requiresId = GloryIds.Legacy
            },
            new GloryNode
            {
                id = GloryIds.FocusWell,
                title = "Focus Well",
                blurb = "+25% Focus max and +20% regen.",
                cost = 6,
                requiresId = GloryIds.Legacy
            },
            new GloryNode
            {
                id = GloryIds.Steel,
                title = "Synergy: Steel",
                blurb = "Each temper rank on relics adds a little tap.",
                cost = 10,
                requiresId = GloryIds.Legacy
            },
            new GloryNode
            {
                id = GloryIds.KeepFortune,
                title = "Keep Fortune",
                blurb = "Fortune ranks survive the next ascend.",
                cost = 12,
                requiresId = GloryIds.KeepMight
            },
            new GloryNode
            {
                id = GloryIds.Hoard,
                title = "Synergy: Hoard",
                blurb = "Each owned relic adds a little gold.",
                cost = 10,
                requiresId = GloryIds.Legacy
            },
            new GloryNode
            {
                id = GloryIds.DeedAngel,
                title = "Deed Angel",
                blurb = "Renown from Deeds is 50% stronger.",
                cost = 15,
                requiresId = GloryIds.Legacy
            },
            new GloryNode
            {
                id = GloryIds.DeepRoad,
                title = "Deep Road",
                blurb = "After Harvest Night the road loops. Cycles pay a little extra.",
                cost = 20,
                requiresId = GloryIds.Legacy,
                requiresBestZone = 9
            },
            new GloryNode
            {
                id = GloryIds.NightMarket,
                title = "Night Market",
                blurb = "Away gold is a bit better. Still weaker than playing.",
                cost = 8,
                requiresId = GloryIds.Legacy
            },
            new GloryNode
            {
                id = GloryIds.BloodOath,
                title = "Blood Oath",
                blurb = "Permanent +25% tap and auto.",
                cost = 25,
                requiresId = GloryIds.KeepMight
            },
            new GloryNode
            {
                id = GloryIds.GiantsDue,
                title = "Giant's Due",
                blurb = "Permanent +50% tap and auto.",
                cost = 50,
                requiresId = GloryIds.BloodOath
            },
        };

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
                return "Ascend first";
            if (!string.IsNullOrEmpty(node.requiresId) && !Has(profile, node.requiresId))
                return "Needs " + Title(node.requiresId);
            if (node.requiresBestZone > 0 && profile.bestZone < node.requiresBestZone)
                return "Reach Harvest Night";
            if (profile.glory < node.cost)
                return "Need " + node.cost + " Glory";
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
