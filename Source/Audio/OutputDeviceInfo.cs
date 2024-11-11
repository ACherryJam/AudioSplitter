using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.AudioSplitter.Audio
{
    [Serializable]
    public struct OutputDeviceInfo
    {
        public int Index;
        public Guid Id;
        public string Name;

        public override int GetHashCode() => Id.GetHashCode();
        public override bool Equals(object obj) => Equals((OutputDeviceInfo)obj);
        public bool Equals(OutputDeviceInfo info) => Id == info.Id;

        public static OutputDeviceInfo DefaultDevice = new OutputDeviceInfo
        {
            Index = 0,
            Id = default,
            Name = default
        };
    }
}
