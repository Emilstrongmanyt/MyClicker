# MyClicker development roadmap

How to improve the game **without** adding a third combat layer, player HP, minions, extra Focus skills, or new field currencies.

Stars stay loop-milestone spend. Relics stay same-stats-per-slot.

---

## Pillars (do not break)

1. **Tapping is the engine.** Forge is the gold spend. Auto is the idle floor, not the fantasy.
2. **Four field currencies:** Gold, Dust, Glory, relics. Stars are not loot.
3. **Three Focus buttons only:** Slam, Fury, Sweep (Reaper is a respecable Sweep spec).
4. **Portrait iOS tap/idle.** One-handed, short sessions, 3h offline cap.

If a feature needs a new bar, a new skill, or a new wallet, it is out of scope.

---

## What the game is now

A solid tap/idle core with **too many prestige planes that say the same thing** (more tap, more gold, more auto).

| Layer | Job it should have | Job it actually has today |
|---|---|---|
| **Forge + Oaths** | This-run power from gold | Works. Engine is healthy. Harvest can still be bought after it does nothing. |
| **Relics + Temper** | Looks + slow Dust sink + collection | Slot stats are real but identical per look. After 24 relics the collection story stops. |
| **Deeds / Renown** | Lifetime trophies | Fine. Missing 16/24 relic deeds. |
| **Mutations** | Permanent Glory dump that survives respec | Competes with the Glory tree for the same wallet with no guidance. |
| **Glory tree** | Ascend shop: Keep / Focus spec / Horde / Gold / Power | Works, but it is a long list. Overlaps Stars on tap/auto/gold/focus. |
| **Star Chart** | Endless specialization web | Right fantasy, slow earn (loop-only), 7 minors before the first notable, two dead tags. Buffs are in the working tree and not shipped. |

The loop (tap → Forge → boss → zone → ascend / endless) is the product. Everything else should **point at that loop**, not duplicate it.

---

## Diagnosis (priority order)

### P0 — Honesty and waste

Players must never spend Gold/Dust/Glory/Stars on a rank that does nothing. Swift overflows. Cleave MAXes. **Harvest does not** (dust 100% ~rank 23, potions ~61, still sells to 200). **Crit overflow is a lie:** 30 ranks = exactly 60%, extra ranks do nothing, Forge still says “next +crit mul”.

War Cry / Godstrike are live spend functions with no button. Star Crit/Offline minors are live multipliers with no nodes. Stats copy still shrugs at relics. HUD tap is 2.25× weaker than the actual tap hit.

### P1 — Teach the planes

A new player sees: tap hint → Forge → Armory → (later) Glory nested in Forge → Stars nested in Forge → Deeds nested in Glory → Stats nested in Armory. Unspent Glory and Stars are easy to miss. First Ascend, first loop, and first Star spend have almost no teaching.

Identity copy is the cheapest fix:

- **Forge** = this run.
- **Glory** = ascend prestige. Keep your run, spec Sweep, buy Power.
- **Stars** = Endless path. Spend loop trophies on a web.

Do not add a seventh plane.

### P2 — Star Chart vs earn rate

~187 nodes. Stars: 1, then 2, then 3… First notable is ~8★ ≈ **four loops**. Full web ≈ 19 loops. That is a fair *endgame* horizon **if** the first 10 Stars feel like a build.

Current (buffed, uncommitted): minors +8%, notables 15–35%, keystones that actually change a rule (Godhand, Sleepless, Second Loop, Reaper Point, Road Mark, Flow). Ship that, then only then consider shortening the inner spoke or giving First Light a real opener.

Do **not** go back to Stars-per-boss. Do **not** turn Stars into gold.

### P3 — Ascend vs Endless

Ascend still wipes cycle unless Keep Road (Glory) or Road Mark (Star). That is a real choice, but it is buried. Most players will Ascend, drop from Endless n to Old Road 0, and feel punished. Keep Road / Road Mark should be obvious the first time they have a cycle to lose.

Keep Swift exists; Crit / Cleave / Harvest / Fury still reset. That is fine if copy says so.

### P4 — Relic endgame

Looks-only per slot is a locked design. The *collection* is the relic game. Milestones now go 4 / 8 / 12 / 16 / 24. Deeds stop at 12. After 24, extra relics are Hoard gold only (and only if Hoard is bought). Need a post-24 Dust/Glory beat that is **not** unique relic stats.

### P5 — Place fantasy

Ten zones, three enemy packs, one background family, tints for cycles 1–3, unique sprites from cycle 4. Combat is readable; the *world* repeats too fast. Bosses should feel like zone events (sting, cue, bar — BGM swap is still missing). Juice: Fury FX exists; per-tap/crit spark and ascend SFX are thin.

### P6 — Session quality

HUD Refresh every frame, no settings (music/sfx), no way back into the creator, no “what did I just unlock” recap after a loop. Offline is intentionally weak (12% × 3h) unless Glory/Night Market/Night Shift stack — keep it that way so idle does not replace tapping.

---

## Phased work

### Phase 0 — Ship what is already true (this week)

- Commit/push the Star node buffs (minors +8%, notables/keystones as in `SYSTEMS.md`).
- TestFlight only when asked.
- Keep `SYSTEMS.md` / this file next to balance changes.

**Done when:** TestFlight build matches the Star blurbs on the Chart.

### Phase 1 — No wasted spends (next)

1. **Harvest overflow or hard MAX.** When dust chance is 100%, extra ranks must raise something (boss dust, relic chance, potion craft discount) *or* the row MAXes like Cleave. Same for potion chance. Also watch Harvest 200 (always-dust + huge relic chance) and Fury 200 (crit mul +50).
2. **Crit must MAX or actually overflow.** Raise the chance cap, lower maxLevel, or convert leftover ranks into crit mul the way the copy already claims.
3. **Delete or wire War Cry / Godstrike.** Prefer delete. Glory should not have secret spend functions.
4. **Either place Crit/Offline Star minors or drop those tags** from EconomyService.
5. **Forge MAX labels** must match `NextRankHelps` for every row. HUD tap should match the 2.25× strike, or the label should say “base”.

**Done when:** buying any visible rank always changes a number on Stats or the row hides.

### Phase 2 — Clarity (next)

1. Forge Stars/Glory buttons show **unspent counts** (Stars already does “Stars N”; Glory should too).
2. One-line identity on each panel header (Forge / Glory / Stars).
3. First-Ascend sting: what persists, what dies, where Glory is.
4. First-loop sting: Stars earned, Forge → Stars, cycle kept or not.
5. Keep Road / Road Mark warning on the Ascend button when `cycle > 0`.
6. Stats: drop the shrug line; list slot stats + collection + shards cleanly.
7. Deed for relics 16 and 24.

**Done when:** a tester can explain Forge vs Glory vs Stars after one loop without a doc.

### Phase 3 — Star Chart feel (after Phase 0 is live)

Only if testers still say the web is empty:

- Shorten inner spoke from 7 minors to 4–5 **or** make the inner notable sit on the spoke.
- Give First Light a tiny real opener (e.g. +5% tap **or** +1 unspent on first grant — not both).
- Region labels / selected-node card: show path cost to the next notable.

Do not inflate Star grants. Do not add a second web.

### Phase 4 — Relic + Dust endgame

- Post-24 collection: either a repeating Dust beat (Temper milestone, cosmetic dye already exists via looks) or a Glory crumb — **not** unique relic affixes.
- Temper preview: next rank cost + next bonus on the Armory row (already close).
- Optional: Armory “collection” strip (4/8/12/16/24) so the milestone is visible without Stats.

### Phase 5 — World and juice

- Distinct zone backgrounds (catalog already has a `background` field; several zones share one).
- Boss sting + boss cue swap (fields exist: `bossCue`).
- Light hit spark on crit / Slam. Keep it cheap on iPhone.
- Loop-clear sting that names the next Endless index and Stars gained.
- Settings: music / sfx / creator re-entry.

### Phase 6 — Balance watch (ongoing, not a feature)

Re-check after each TestFlight, in this order:

1. Tap still beats auto when the player is tapping (`2.25` vs `0.40` is the lever).
2. Cycle seam: Harvest Night 0 → Old Road 1 is a step up, not a dump (HP 5.2 vs zone 5.1).
3. Unspent Tithe vs spending Glory (hoarding should not beat the tree).
4. Iron Pulse / Titan Heart vs Star Godhand (Glory Power is allowed to be bigger; Stars should be the *shape*).
5. Sweep vs Reaper: horde clear vs boss check. Respec must stay cheap to find.
6. Offline stays worse than playing. Unspent Glory `×0.008` can dwarf Night Market — watch hoarding.
7. Blood Tithe 40 ≈ ×22 run gold vs Fortune’s additive 12%. Oaths should not obsolete Forge.
8. HUD gold/s is auto-only; tapping looks weaker than it is.

---

## Explicit non-goals

- Player HP, shields, deaths, or a fail state.
- Minions, pets, or a second attacker.
- A fourth Focus skill or a player combat skill tree.
- Stars as kill/boss loot.
- Unique stats per relic look.
- New currencies (gems, keys, energy, tokens).
- Ads / IAP until the loop is self-explanatory (separate decision).
- Cookie-style random buildings.

---

## Suggested next three moves

1. **Ship Star buffs** (already in the working tree).
2. **Harvest MAX / overflow** so Forge cannot eat gold for nothing.
3. **Ascend + first-Star teaching** (cycle warning, Forge vs Glory vs Stars copy, relic 16/24 deeds).

After that, listen to TestFlight: if Stars still feel empty, do Phase 3 spoke work. If the world feels samey, do Phase 5. Do not start both at once.
