using UnityEngine;

namespace Bufe
{
    /// <summary>Where/how an event plays.</summary>
    public enum EventSurface
    {
        /// <summary>Event-screen modal at day end (FamilyEvent).</summary>
        Night,
        /// <summary>In-world prompt at the büfe counter during the day (FactionInteraction).</summary>
        DayBufe
    }

    /// <summary>
    /// Base for all story events. Subclasses declare their surface:
    ///   FamilyEvent        → Night   (event-screen modal)
    ///   FactionInteraction → DayBufe (in-world counter prompt)
    ///
    /// A choice's followUpEvent is typed as GameEvent, so any event can schedule any other
    /// regardless of surface - the scheduler routes by Surface. This makes day→night,
    /// night→day, and day→day (e.g. accept package → collect it later) all work uniformly.
    /// </summary>
    public abstract class GameEvent : ScriptableObject
    {
        public abstract EventSurface Surface { get; }
    }
}
