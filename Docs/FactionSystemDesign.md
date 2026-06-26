# Faction System — Technical Design

Status: Draft for review
Author: design pass with Senior Unity Dev
Target scope: Day-60 narrative loop, Papers-Please-style branching endings

---

## 1. Goal

Add three factions — **Taxi**, **Police**, **Neighborhood** — each with a single
shop-wide relationship value (0–10) that the player moves through actions, quests,
and serving faction members. Faction standing drives **conditional events** and,
ultimately, **branching endings** (either an early ending triggered by extreme
standing, or the Day-60 "final day" event whose outcome depends on all relationships).

Design principle: **generalize the existing family/relationship/event systems into a
shared core; factions and family ride the same rails.** No parallel silo.

---

## 1A. DEMO SCOPE — July 29 (Trade-Show Booth) ⭐ ACTIVE PRIORITY

> Everything below in §§2–10 is the **full system**. For the July 29 demo we build a
> deliberate *sliver* of it and defer the rest. This section is the source of truth
> until the demo ships; the full design resumes post-demo.

### Demo context (drives every scope call)
- **Format:** trade-show booth. Stranger sits down cold, dev present, plays once, leaves.
- **Session length:** ~20 minutes = **5 in-game days**.
- **Hero faction:** **Taxi only.** Police + Neighborhood bars exist but are cosmetic
  (no quests, minimal/no movement) for the demo.
- **Core loop status:** works fully; **needs polish**, not rebuilding.

### Booth-specific requirements (easy to forget, non-negotiable)
- **Never crashes / never soft-locks.** Stability > features.
- **Clean reset between players** — one action back to a fresh Day 1
  (`GameManager.RestartGame()` scene reload is the basis).
- **Instant readability** — graspable in ~30s with a one-line nudge from the dev.
- **Instant start** — skip menus / minimal front-end.
- Replayability does **not** matter (each player plays once) → the arc is **hand-scripted**,
  not systemic. This is the biggest time-saver.

### The demo arc — self-contained 5-day Taxi storyline
Cause → effect must land **inside** the 5-day window (booth players never see Day 60).

| Day | Beat |
|---|---|
| 1 | Learn loop (cook/serve/money/rep). Designed **taxi driver** appears as a customer. 3 faction bars visible. Family pressure present (stakes). |
| 1 eve | **Taxi quest offered** via event modal — accept/reject (e.g. "let drivers run a tab" / "stash a package"). The one real accept/reject quest. |
| 2–3 | Normal loop. Serving taxi members nudges Taxi bar up; refusing nudges down. Quest ticking. |
| 3–4 eve | Quest **resolves** → consequence event branching on player actions. Good: Taxi bar high + small reward + warm line. Bad/reject: Taxi bar low + threat ("we'll remember this"). |
| 5 | Short visible **faction reaction** payoff → end on "To be continued — wishlist on Steam" card. |

### Faction visibility — where the player sees faction state (CORRECTED)
There is **no persistent HUD bar and no mid-cooking toast.** The kiosk has no
continuous cooking loop — the player clicks a customer and decides at a discrete
**order prompt** (Accept/Reject). So faction relationships only ever change at
**discrete modal decision points**, and the bar is shown *there*, animating the delta:

| Faction member appears… | Surface | Notes |
|---|---|---|
| At the büfe, as a customer | **Extended order prompt** | Same in-world presentation as the screenshot — 3D character stays visible, dim vignette, dialogue + buttons at the bottom. NOT a screen-covering modal. Richer than Accept/Reject: dynamic choice buttons carrying faction effects. Art pass comes later. |
| At night | **Event modal** (`EventScreenUI`) | Existing night-event screen, unchanged presentation. |
| (never) | No HUD, no toast | Decommissioned ideas. |

Demo faction visits are **pure dialogue/choice — no cooking.** The member talks, the
player picks a choice, the faction bar reacts inline, the member leaves.

`FactionBarUI` (built earlier for a HUD/toast) is **repurposed**: it becomes the small
inline bar shown *inside* these prompts, animating the delta (mirroring how
`EventMemberUI` animates family bars). No standalone HUD instance.

### Büfe order-prompt extension — integration plan
- A faction member is a **special customer** carrying faction-interaction data
  (dialogue line + list of choices, each with faction effects). Spawned via a scripted
  hook, not the random `CreateNormalCustomer` pool.
- `OrderManager.HandleClickInput` → `WaitingForOrder`: branch — if the active customer
  is a faction member, call `ShowFactionPrompt()` instead of `ShowOrderPrompt()`.
- `CustomerOrderUI` gains a **choice mode**: dialogue text + dynamic choice buttons
  (reusing the `choiceButtonPrefab` pattern from `EventScreenUI`) + an inline
  `FactionBarUI`. Keeps the existing Accept/Reject/Close path untouched for normal
  customers.
- Choice selected → apply faction delta via `FactionManager.Modify` → bar animates →
  member leaves (`SendCustomerAway`).
- **Branching consequences:** each `EventChoice` can set `followUpEvent` + `followUpDelay`.
  `OrderManager` calls `EventScreenManager.ScheduleEvent(...)` so a büfe choice schedules a
  night event N days later. This gives full Day-1→Day-3 branching (accept → warm event,
  refuse → threat event) with **no condition engine** — the deferred §4 layer stays deferred.

### Faction member spawn timing
- `FactionSpawnEntry`: `day` + `spawnAfterCustomers` (0 = first customer, N = appears after
  N normal customers that day) + `interaction`. `OrderManager` tracks `_normalCustomersToday`
  (reset on day end).
- **Closing-time guarantee:** if a scheduled member hasn't appeared by closing
  (`GetPendingFactionSpawnToday`, ungated), it's force-spawned as the last customer via
  `OnClosingTime` / `SendCustomerAway` — so a member always appears on its day.

### Item give / collect (expandable handoff)
Both directions reuse existing systems — no parallel item path:
- **Grant:** `EventEffect.grantedItemPrefab` (an `InteractableItem`). Applied in the büfe path
  (`ApplyEffects` → `GrantItem`), spawned at `itemGrantPoint` on the counter (falls back to the
  ticket point). Player picks it up / stashes / re-grabs via the existing `PlayerHand` — no new
  code for stashing. Night events ignore this field.
- **Collect:** `FactionInteraction.kind = Collect` + `requiredItem` (an `OrderItem`) +
  `onCollectedEffects`. Clicking the member while holding a matching item validates via
  **`OrderValidator.ValidateSlot`** (same path as order delivery; identity via
  `PremadeItem.definition`), consumes it (`DestroyHeldItem`), and applies effects. Empty-handed
  click → reminder line; the member keeps waiting.
- **Expandable:** any item (not just the package), effects are the shared `EventEffect` list
  (faction/money/grant), new interaction kinds are new enum cases.
- **Demo loop:** Day-1 taxi *Offer* grants the package box → player stashes it → later-day taxi
  *Collect* takes it back → +Taxi payoff.
- **Asset needed (not code):** a package prefab = `PremadeItem` + an `OrderItem` definition;
  the grant prefab and the collect `requiredItem` both reference that definition.
- **Known limit (demo-ok):** `onCollectedEffects` can't schedule a follow-up night event
  (`followUpEvent` lives on `EventChoice`, not `EventEffect`). Add later if a collect needs to
  trigger a night beat.

### Demo build approach — SHORTCUT, deliberate tech debt
**Defer the §3 generic `RelationshipTarget` refactor, §4 condition engine, §5 QuestManager,
and §7 endings.** None of them show at a booth. Instead:

- Add a small **`FactionManager` + `FactionData`** (3 factions, 0–10 value, Low/Med/High
  bar) — pragmatically *parallel* to `RelationshipManager`, **not** generalized yet.
- Extend **`EventEffect`** so a delta can optionally target a faction (in addition to the
  existing `targetMember`).
- Build the quest as **authored content**: a `FamilyEvent` with two choices; Accept
  schedules the consequence event N days later — `EventScreenManager` **already supports
  this** via `followUpEvent`/`followUpDelay`. No condition engine needed; the arc is
  scripted on fixed days.

Result: the "real quest" is ~90% authored data, ~10% new code.
**This shortcut is intentional and reversible** — post-demo we replace `FactionManager`
with the generalized core in §3 and migrate. Recorded here so it reads as debt-by-design,
not accident.

### Cut line for the demo
**OUT:** generic relationship refactor · condition engine · QuestManager system ·
Day-60 final event · ending tree / `EndingData` · save/persistence · world-action drivers
· Police/Neighborhood quests · multiple faction beats.
**IN:** polished core loop · family pressure for stakes · 3 faction bars (Taxi active) ·
1 scripted designed taxi-driver NPC · 1 real accept/reject Taxi quest with a consequence
that lands by Day 5 · booth UX (instant start, reset, readability) · end card.

### Month plan (4 weeks to July 29)
| Window | Focus |
|---|---|
| Wk1 (Jun 22–29) | Core-loop polish + booth hardening. `FactionManager`/`FactionData` + 3 bars. Clean Day 1→5→reset running. **Stability & feel first.** |
| Wk2 (Jun 30–Jul 6) | `EventEffect` faction targeting. Serving-faction-member hook moves Taxi bar. Author taxi quest offer + branch events. |
| Wk3 (Jul 7–13) | Author full 5-day arc content. Designed taxi-driver NPC. Tune family pressure. End card. |
| Wk4 (Jul 14–20) | Balance 20-min pacing. Booth UX polish. Juice / SFX / feedback. |
| Buffer (Jul 21–28) | Bug bash, play 50×, **freeze — no new features.** |

Buffer week is non-negotiable for a booth build.

---

## 2. What already exists (reused, not rebuilt)

| Capability | Existing class | Role in faction system |
|---|---|---|
| 0–10 relationship value + Low/Med/High level | `RelationshipManager` | Becomes generic; tracks factions too |
| Designed, authored entity (SO) | `FamilyMemberData` | Sibling: `FactionData` |
| Authored event w/ choices, deltas, follow-ups | `FamilyEvent` / `EventChoice` / `EventEffect` | Becomes generic `RelationshipEvent` |
| Day-end event queue (scheduled + random) | `EventScreenManager` | Gains a **condition** gate |
| Modal portrait + text + choice UI | `EventScreenUI` | Reused for faction quests/events |
| Per-faction member appearing at kiosk | `CustomerFactory.CreateSpecialCustomer` (planned hook) | Spawns faction members |
| Game-over / ending entry point | `GameManager.TriggerGameOver` | Extended for multi-ending |

---

## 3. Core refactor: generic relationship target

Introduce a shared abstraction so one `EventEffect` can hit a faction **or** a family
member, and one manager tracks both.

```csharp
// New base. FamilyMemberData and FactionData both derive from it.
public abstract class RelationshipTarget : ScriptableObject
{
    public string displayName;
    public Sprite portrait;
}

public class FamilyMemberData : RelationshipTarget { /* existing fields */ }

[CreateAssetMenu(fileName = "Faction", menuName = "Kosem Bufe/Faction")]
public class FactionData : RelationshipTarget
{
    public FactionId id;                 // Taxi, Police, Neighborhood
    [Range(0,10)] public int startingRelationship = 5;
    public List<RelationshipTarget> members;   // designed NPCs belonging to this faction
}

public enum FactionId { Taxi, Police, Neighborhood }
```

`RelationshipManager` changes:
- Key the dictionary on `RelationshipTarget` instead of `FamilyMemberData`.
- `Modify(RelationshipTarget, int)`, `GetValue`, `GetLevel`, `OnRelationshipChanged`
  all become target-generic.
- Per-target starting value + thresholds (factions and family may want different
  thresholds; move thresholds onto the target or keep global defaults with per-target
  override).

`EventEffect.targetMember` → `RelationshipTarget target`. **Backward compatible**:
existing FamilyEvent assets still reference family members, which are now
`RelationshipTarget`s. (Verify Unity SO references survive the base-class change — see
Risks §9.)

> Pragmatic fallback if family relationships are considered "frozen": ship a parallel
> `FactionManager` mirroring `RelationshipManager`. Cheaper now, but every ending
> condition and cross-effect gets written twice. Recommended path is the shared core.

---

## 4. The missing piece: a condition layer

Current event gating = `minDay` + random `triggerChance`. Factions need
**state-conditional** triggering (the whole point). Add a small, data-driven condition
block evaluated before an event enters the day queue.

```csharp
[Serializable]
public class EventCondition
{
    public enum Kind { FactionLevelAtLeast, FactionLevelAtMost, FactionValueRange,
                       DayAtLeast, QuestCompleted, QuestFailed, FlagSet, FlagUnset }

    public Kind kind;
    public FactionData faction;     // for faction kinds
    public RelationshipLevel level; // for level kinds
    public int minValue, maxValue;  // for range / day kinds
    public string questOrFlagId;    // for quest/flag kinds
}
```

`RelationshipEvent` (renamed `FamilyEvent`) gains:
```csharp
public List<EventCondition> conditions;  // ALL must pass to enter the queue
public List<EventEffect> finalEffects;   // applied when event fully resolves (optional)
public string endingId;                  // non-empty => resolving this event ends the game
```

`EventScreenManager.BuildTodayQueue` adds one check: `if (!AllConditionsPass(evt)) continue;`

This single addition expresses everything in the brief:
- **Torch attack**: condition `Taxi <= Low`, scheduledDay or follow-up → ending event.
- **Police saves you**: separate event, condition `Taxi <= Low AND Police >= High` →
  fires *instead*, no ending. (Order events so the "save" outranks the "attack", or
  make the attack a follow-up that re-checks the save condition on the day it fires.)
- **Join the taxis (early ending)**: condition `Taxi >= High AND quest:taxi_initiation
  completed AND day >= N` → ending event with `endingId = "join_taxi"`.

Endings become **authored data**, not hardcoded C#. Good while the story isn't final.

---

## 5. Quest system (lightweight)

A quest is a `RelationshipEvent` offer with Accept / Reject choices. Keep it minimal —
no quest graph framework for Day-60 scope.

```csharp
[CreateAssetMenu(menuName = "Kosem Bufe/Faction Quest")]
public class FactionQuest : ScriptableObject
{
    public string questId;
    public FactionData faction;
    public RelationshipEvent offerEvent;       // Accept/Reject modal
    public int deadlineDays;                    // days to complete after accepting
    public RelationshipEvent completionEvent;   // success path
    public RelationshipEvent failureEvent;      // missed-deadline path
}

public class QuestManager : MonoBehaviour   // singleton, like the others
{
    // questId -> Active/Completed/Failed/Rejected
    public QuestState GetState(string questId);
    public void Accept(FactionQuest q);   // schedules completion check on +deadlineDays
    public void Complete(string questId); // sets flag, applies completionEvent effects
}
```

`QuestManager` exposes state to `EventCondition` (`QuestCompleted`/`QuestFailed`) so
quest outcomes feed the ending engine directly. Accepting schedules a check via the
existing `_scheduledEvents` mechanism in `EventScreenManager`.

---

## 6. Relationship drivers (per your selection)

1. **Quest accept/reject + outcomes** — `EventEffect` deltas on offer choices and on
   completion/failure events. Primary mover.
2. **Serving faction members** — designed faction NPCs spawn via
   `CustomerFactory.CreateSpecialCustomer(FactionData)`. The order-result hook (where
   shop reputation currently changes) also calls
   `RelationshipManager.Modify(faction, ±delta)`. Perfect delivery raises, bad/refused
   lowers that faction.
3. **Day-end events** — conditional/random `RelationshipEvent`s, same flow as today's
   family events.

(World actions — pricing, refusals — deferred; easy to add later as effect sources.)

---

## 7. Endings & the Day-60 final event

### 7.1 Day cap
Add to `GameConfig`:
```csharp
[Header("Run Length")]
public int totalDays = 60;          // Day-60 final event fires here
```
`DayManager.BeginNextDay` checks `if (currentDay > totalDays) -> trigger final event`.

### 7.2 Final-day event
A `RelationshipEvent` with `scheduledDay = 60` and no normal choices — it branches into
one of several ending events by faction state. Implement as either:
- a router event whose follow-ups are gated by `EventCondition`s, or
- a small `EndingResolver` that picks the highest-priority ending whose conditions pass.

### 7.3 Early endings
Any ending event with a non-empty `endingId` that resolves before Day 60 (torch attack,
taxi recruitment, etc.) calls into the ending pipeline immediately.

### 7.4 Multi-ending UI
Extend the game-over path:
```csharp
[CreateAssetMenu(menuName = "Kosem Bufe/Ending")]
public class EndingData : ScriptableObject
{
    public string endingId;     // "ending_6_torched", "join_taxi", ...
    public string title;
    [TextArea] public string body;
    public Sprite art;
}
```
`GameManager.TriggerEnding(EndingData)` replaces/wraps `TriggerGameOver(string)`.
`GameOverUI.ShowEnding(EndingData)` renders title/body/art. (Current `ShowGameOver(reason)`
stays for bankruptcy/reputation fail-states, or those become `EndingData` too.)

---

## 8. Build phases

1. **Core refactor** — `RelationshipTarget` base; generalize `RelationshipManager` &
   `EventEffect`; rename `FamilyEvent`→`RelationshipEvent`. Verify family still works.
2. **FactionData + UI** — create 3 faction SOs; faction relationship bars (reuse
   `FamilyRowUI`/`ReputationUI` pattern).
3. **Condition layer** — `EventCondition` + gate in `BuildTodayQueue`.
4. **Quest system** — `FactionQuest` + `QuestManager` + condition hooks.
5. **Serving hook** — `CreateSpecialCustomer` + faction delta on order result.
6. **Endings** — `totalDays`, final-day router, `EndingData`, `TriggerEnding`, UI.
7. **Content** — author quests/events/endings (your job, all in inspector).

Phases 1–3 = "core systems". Phases 1–6 + a couple authored quests/one ending = vertical
slice.

---

## 9. Risks / open questions

- **SO reference migration**: changing `FamilyMemberData`'s base to `RelationshipTarget`
  and retyping `EventEffect.targetMember` — confirm existing `.asset`/event references
  deserialize cleanly. Likely fine (same instance, wider field type) but test before
  committing content.
- **Save/serialization**: relationship values, quest states, and scheduled events are
  currently runtime-only (`HasTriggered` is `[NonSerialized]`). If a save system is
  planned, faction state must persist — note for later, out of scope now.
- **Event priority/ordering**: when an attack event and a save event are both eligible,
  ordering matters. Decide: explicit priority field vs. authored follow-up re-check.
- **Threshold ownership**: per-faction thresholds vs. global defaults — affects how Low/
  Med/High maps for each faction's bar.
- **Member vs. faction relationships**: brief says no per-member relationship, only
  faction-wide. `FactionData.members` is purely for spawning/identity, not tracked
  individually. Confirmed.

---

## 10. Decisions locked

- Generalize shared core (not parallel silo). ✅
- Relationship drivers: quests + serving members + day-end events. ✅
- Faction-wide relationships only; members are designed but not individually tracked. ✅
