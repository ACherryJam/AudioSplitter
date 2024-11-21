using System;
using Celeste.Mod.AudioSplitter.Module;
using FMOD;

namespace Celeste.Mod.AudioSplitter.Audio
{
    [Serializable]
    public struct OutputDeviceInfo : IEquatable<OutputDeviceInfo>
    {
        public int Index;
        public Guid Id;
        public string Name;

        public override int GetHashCode() => Id.GetHashCode();
        public override bool Equals(object obj) => Equals((OutputDeviceInfo)obj);
        public bool Equals(OutputDeviceInfo info) => Id == info.Id;

        public static bool operator ==(OutputDeviceInfo left, OutputDeviceInfo right) => left.Equals(right);
        public static bool operator !=(OutputDeviceInfo left, OutputDeviceInfo right) => !left.Equals(right);

        public RESULT Apply(FMOD.System system)
        {
            Logger.Verbose(nameof(AudioSplitterModule), $"Setting device (Id {Id}, index {Index}) to system {system.getRaw()}");
            return system.setDriver(this.Index);
        }

        public RESULT Apply(FMOD.Studio.System system)
        {
            RESULT result = system.getLowLevelSystem(out FMOD.System lowLevelSystem);
            if (result != RESULT.OK)
                return result;
            return Apply(lowLevelSystem);
        }

        public static OutputDeviceInfo DefaultDevice = new OutputDeviceInfo
        {
            Index = 0,
            Id = default,
            Name = default
        };
    }
}
