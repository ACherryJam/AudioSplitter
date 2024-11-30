using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste.Mod.AudioSplitter.Module;

namespace Celeste.Mod.AudioSplitter.Volume
{
    public static class VolumeChangeInputListener
    {
        public static readonly ChannelInputListener SFX = new(
            AudioSplitterModule.Settings.DecreaseSFXVolumeBinding,    
            AudioSplitterModule.Settings.IncreaseSFXVolumeBinding
        );

        public static readonly ChannelInputListener Music = new(
            AudioSplitterModule.Settings.DecreaseMusicVolumeBinding,
            AudioSplitterModule.Settings.IncreaseMusicVolumeBinding
        );

        public static readonly List<ChannelInputListener> Listeners = new() { SFX, Music };
    }
}
