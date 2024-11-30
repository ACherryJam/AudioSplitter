using System;
using Celeste.Mod.AudioSplitter.Module;

namespace Celeste.Mod.AudioSplitter.UI.Volume
{
    public enum VolumeViewTypes
    {
        None = 0,
        Minimalistic = 1
    }

    public static class VolumeViewFactory
    {
        public static VolumeView Create(VolumeViewTypes type)
        {
            var pos = AudioSplitterModule.Settings.VolumeViewPosition;
            var origin = AudioSplitterModule.Settings.VolumeViewOrigin;

            return type switch
            {
                VolumeViewTypes.None => new EmptyVolumeView(pos, origin),
                VolumeViewTypes.Minimalistic => new MinimalisticVolumeView(pos, origin),
                _ => throw new ArgumentException($"Got unexpected type of VolumeView {type}")
            };
        }
    }
}
