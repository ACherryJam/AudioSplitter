using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FMOD;
using FMOD.Studio;
using CelesteAudio = global::Celeste.Audio;

namespace Celeste.Mod.AudioSplitter.Audio
{
    public class EventCache
    {
        private Dictionary<string, EventDescription> cachedEventDescriptions = new();

        private FMOD.Studio.System system;

        public EventCache(FMOD.Studio.System system)
        {
            this.system = system;
        }

        public void LoadEventDescription(string path)
        {
            if (path is null)
                return;

            EventDescription eventDescription = null;
            if (path == null || cachedEventDescriptions.TryGetValue(path, out eventDescription))
                return;

            RESULT result;

            if (CelesteAudio.cachedModEvents.TryGetValue(path, out eventDescription))
            {
                eventDescription.getID(out Guid id).CheckFMOD();
                result = system.getEventByID(id, out eventDescription);
            }
            else if (path.StartsWith("guid://"))
            {
                result = system.getEventByID(Guid.Parse(path.AsSpan(7)), out eventDescription);
            }
            else
            {
                result = system.getEvent(path, out eventDescription);
            }

            if (result == RESULT.OK)
            {
                eventDescription.loadSampleData();
                cachedEventDescriptions.Add(path, eventDescription);
            }
            else
            {
                if (result != RESULT.ERR_EVENT_NOTFOUND)
                {
                    throw new Exception("FMOD getEvent failed: " + result.ToString());
                }
                if (!(path == "null") && !(path == "event:/none"))
                {
                    Logger.Warn("Audio", "Event not found: " + path);
                }
            }
        }

        public void ReleaseUnusedDescriptions()
        {
            List<string> descriptionsToRemove = new();
            foreach ((string path, EventDescription desc) in CelesteAudio.cachedEventDescriptions)
            {
                desc.getInstanceCount(out int num);
                if (num <= 0)
                {
                    desc.unloadSampleData();
                    descriptionsToRemove.Add(path);
                }
            }
            foreach (string text in descriptionsToRemove)
            {
                cachedEventDescriptions.Remove(text);
            }
        }
    }
}
