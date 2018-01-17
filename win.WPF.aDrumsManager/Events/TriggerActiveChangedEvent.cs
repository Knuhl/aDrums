using System.Collections.Generic;
using aDrumsLib;
using Prism.Events;

namespace win.WPF.aDrumsManager.Events
{
    public class TriggerActiveChangedEvent : PubSubEvent<KeyValuePair<Pins, bool>>
    {
    }
}
