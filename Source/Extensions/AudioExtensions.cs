using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.AudioSplitter.Extensions
{
    public static class AudioExtensions
    {
        public static string GetEventPath(Guid id)
        {
            return global::Celeste.Audio.cachedPaths.TryGetValue(id, out string path) ? path : $"guid://{id}";
        }
    }
}
