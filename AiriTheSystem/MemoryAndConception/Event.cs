using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Airi.TheSystem.Conception
{
    /// <summary>
    /// Provides a decription for things happen/indexed by time; Events are infetestimally identifiable processes that Airi can perceive and it constitutes the majority of Airi's experience
    /// Events uniquely identifies an Intelligent Being's personality and memory.
    /// Events contain information only about things happened: No explicit time is used to characterize an event. An event describes only things that HAPPEN, 
    /// Specturm of EVents: Time (can be explicit or implied by order of events), location, people, action, effect, consequence -- All such information are embeded in one descriptive sentence that Airi generated for herself
    /// </summary>
    internal class EventMemory
    {
        private class Event
        {

        }
        List<Event> Experience; // Encounters, personality etc.
        Dictionary<string, Event> ThemedEvents; // Provides one layer of abstraction for non-personal events memory

        void AddEvent(/**/)
        {
            throw new NotImplementedException();
        }

        void ModifyEvent(/**/)
        {
            throw new NotImplementedException();
        }

        // Find some event similar
        void MatchEvent(/**/)
        {
            throw new NotImplementedException();
        }
    }
}
