using System.Collections.Generic;
using MyClicker.App;
using UnityEngine;

namespace MyClicker.Economy
{
    public enum StarKind
    {
        Minor,
        Notable,
        Keystone
    }

    public static class StarIds
    {
        public const string FirstLight = "first_light";
        public const string IronFinger = "iron_finger";
        public const string HeavyTap = "heavy_tap";
        public const string CritSpark = "crit_spark";
        public const string Pulse = "pulse";
        public const string Nail = "nail";
        public const string TapEdge = "tap_edge";
        public const string Godhand = "godhand";
        public const string Tick = "tick";
        public const string NightShift = "night_shift";
        public const string Overspin = "overspin";
        public const string Metronome = "metronome";
        public const string Anvil = "anvil";
        public const string AutoEdge = "auto_edge";
        public const string Sleepless = "sleepless";
        public const string Wellspring = "wellspring";
        public const string CheapSlam = "cheap_slam";
        public const string LongFury = "long_fury";
        public const string Wide = "wide";
        public const string Point = "point";
        public const string FocusEdge = "focus_edge";
        public const string Flow = "flow";
        public const string Tithe = "tithe";
        public const string BossPurse = "boss_purse";
        public const string RelicMagnet = "relic_magnet";
        public const string Merchant = "merchant";
        public const string Smith = "smith";
        public const string GoldEdge = "gold_edge";
        public const string HoardStar = "hoard_star";
        public const string RoadMark = "road_mark";
        public const string DepthSense = "depth_sense";
        public const string Thick = "thick";
        public const string Thin = "thin";
        public const string CycleGold = "cycle_gold";
        public const string SecondLoop = "second_loop";
    }

    public static class StarTags
    {
        public const string Tap = "tap";
        public const string Auto = "auto";
        public const string Gold = "gold";
        public const string Regen = "regen";
        public const string Relic = "relic";
        public const string BossGold = "boss_gold";
    }

    public class StarNode
    {
        public string id;
        public string title;
        public string blurb;
        public string iconId;
        public string statTag;
        public string exclusiveWith;
        public string effectId;
        public StarKind kind = StarKind.Minor;
        public int cost = 1;
        public float x;
        public float y;
        public string[] neighbors = System.Array.Empty<string>();
    }

    public static class StarTree
    {
        static StarNode[] _all;
        static Dictionary<string, StarNode> _byId;

        public static StarNode[] All
        {
            get
            {
                Ensure();
                return _all;
            }
        }

        public static void Ensure()
        {
            if (_all != null && _all.Length > 0)
                return;
            _all = StarLayout.Build();
            _byId = new Dictionary<string, StarNode>(_all.Length);
            for (int i = 0; i < _all.Length; i++)
            {
                if (_all[i] != null && !string.IsNullOrEmpty(_all[i].id))
                    _byId[_all[i].id] = _all[i];
            }
        }

        public static StarNode Find(string id)
        {
            Ensure();
            if (string.IsNullOrEmpty(id) || _byId == null)
                return null;
            StarNode node;
            return _byId.TryGetValue(id, out node) ? node : null;
        }

        public static bool Has(PlayerProfile profile, string id)
        {
            if (profile == null || string.IsNullOrEmpty(id) || profile.starNodes == null)
                return false;
            for (int i = 0; i < profile.starNodes.Length; i++)
            {
                if (profile.starNodes[i] == id)
                    return true;
            }

            return false;
        }

        public static bool Unlock(PlayerProfile profile, string id)
        {
            if (profile == null || string.IsNullOrEmpty(id) || Has(profile, id))
                return false;
            int n = profile.starNodes != null ? profile.starNodes.Length : 0;
            var next = new string[n + 1];
            if (n > 0)
                System.Array.Copy(profile.starNodes, next, n);
            next[n] = id;
            profile.starNodes = next;
            return true;
        }

        public static int Spent(PlayerProfile profile)
        {
            Ensure();
            if (profile == null || profile.starNodes == null)
                return 0;
            int total = 0;
            for (int i = 0; i < profile.starNodes.Length; i++)
            {
                var node = Find(profile.starNodes[i]);
                if (node != null && node.cost > 0)
                    total += node.cost;
            }

            return total;
        }

        public static int Unspent(PlayerProfile profile)
        {
            if (profile == null)
                return 0;
            return Mathf.Max(0, profile.starEarned - Spent(profile));
        }

        public static int MinorRank(PlayerProfile profile, string tag)
        {
            Ensure();
            if (profile == null || profile.starNodes == null || string.IsNullOrEmpty(tag))
                return 0;
            int n = 0;
            for (int i = 0; i < profile.starNodes.Length; i++)
            {
                var node = Find(profile.starNodes[i]);
                if (node != null && node.kind == StarKind.Minor && node.statTag == tag)
                    n++;
            }

            return n;
        }

        public static int Respec(PlayerProfile profile)
        {
            if (profile == null)
                return 0;
            int spent = Spent(profile);
            profile.starNodes = profile.starEarned > 0 ? new[] { StarIds.FirstLight } : new string[0];
            return spent;
        }

        public static bool CanBuy(PlayerProfile profile, StarNode node)
        {
            if (profile == null || node == null)
                return false;
            if (Has(profile, node.id))
                return false;
            if (!string.IsNullOrEmpty(node.exclusiveWith) && Has(profile, node.exclusiveWith))
                return false;
            if (node.cost > 0 && Unspent(profile) < node.cost)
                return false;
            if (node.cost <= 0)
                return profile.starEarned > 0;
            if (node.id == StarIds.FirstLight)
                return profile.starEarned > 0;
            if (node.neighbors == null || node.neighbors.Length == 0)
                return false;
            for (int i = 0; i < node.neighbors.Length; i++)
            {
                if (Has(profile, node.neighbors[i]))
                    return true;
            }

            return false;
        }

        public static string LockReason(PlayerProfile profile, StarNode node)
        {
            if (node == null)
                return "";
            if (Has(profile, node.id))
                return "Owned";
            if (!string.IsNullOrEmpty(node.exclusiveWith) && Has(profile, node.exclusiveWith))
                return "Picked " + Title(node.exclusiveWith);
            if (node.cost > 0 && Unspent(profile) < node.cost)
                return "Need " + node.cost + "★";
            if (!CanBuy(profile, node))
                return "Path to an owned star";
            return "";
        }

        public static string Title(string id)
        {
            var node = Find(id);
            return node != null ? node.title : id;
        }

        public static bool PruneUnknown(PlayerProfile profile)
        {
            Ensure();
            if (profile == null || profile.starNodes == null || profile.starNodes.Length == 0)
                return false;
            int keep = 0;
            for (int i = 0; i < profile.starNodes.Length; i++)
            {
                if (Find(profile.starNodes[i]) != null)
                    keep++;
            }

            if (keep == profile.starNodes.Length)
                return false;
            var next = new string[keep];
            int w = 0;
            for (int i = 0; i < profile.starNodes.Length; i++)
            {
                if (Find(profile.starNodes[i]) == null)
                    continue;
                next[w++] = profile.starNodes[i];
            }

            profile.starNodes = next;
            return true;
        }

        public static int PathCost(PlayerProfile profile, string id)
        {
            Ensure();
            var target = Find(id);
            if (profile == null || target == null)
                return -1;
            if (Has(profile, id))
                return 0;

            var dist = new Dictionary<string, int>();
            var q = new Queue<string>();
            if (profile.starNodes != null)
            {
                for (int i = 0; i < profile.starNodes.Length; i++)
                {
                    string owned = profile.starNodes[i];
                    if (string.IsNullOrEmpty(owned) || Find(owned) == null)
                        continue;
                    dist[owned] = 0;
                    q.Enqueue(owned);
                }
            }

            if (dist.Count == 0 && profile.starEarned > 0)
            {
                dist[StarIds.FirstLight] = 0;
                q.Enqueue(StarIds.FirstLight);
            }

            int best = int.MaxValue;
            while (q.Count > 0)
            {
                string cur = q.Dequeue();
                int d;
                if (!dist.TryGetValue(cur, out d) || d >= best)
                    continue;
                var node = Find(cur);
                if (node == null || node.neighbors == null)
                    continue;
                for (int i = 0; i < node.neighbors.Length; i++)
                {
                    var next = Find(node.neighbors[i]);
                    if (next == null)
                        continue;
                    if (!string.IsNullOrEmpty(next.exclusiveWith) && Has(profile, next.exclusiveWith))
                        continue;
                    int extra = Has(profile, next.id) ? 0 : Mathf.Max(0, next.cost);
                    int nd = d + extra;
                    if (next.id == target.id && nd < best)
                        best = nd;
                    int old;
                    if (dist.TryGetValue(next.id, out old) && old <= nd)
                        continue;
                    dist[next.id] = nd;
                    q.Enqueue(next.id);
                }
            }

            return best == int.MaxValue ? -1 : best;
        }

        public static StarNode NearestNotable(PlayerProfile profile, StarNode from)
        {
            Ensure();
            if (from == null)
                return null;
            if (from.kind != StarKind.Minor)
                return null;
            var seen = new HashSet<string> { from.id };
            var q = new Queue<string>();
            q.Enqueue(from.id);
            while (q.Count > 0)
            {
                var node = Find(q.Dequeue());
                if (node == null || node.neighbors == null)
                    continue;
                for (int i = 0; i < node.neighbors.Length; i++)
                {
                    string id = node.neighbors[i];
                    if (seen.Contains(id))
                        continue;
                    seen.Add(id);
                    var next = Find(id);
                    if (next == null)
                        continue;
                    if (next.kind != StarKind.Minor && (profile == null || !Has(profile, next.id)))
                        return next;
                    q.Enqueue(id);
                }
            }

            return null;
        }
    }
}
