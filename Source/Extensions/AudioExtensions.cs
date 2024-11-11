using System;

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
