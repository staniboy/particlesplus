using System.Collections.Generic;
using Vintagestory.API.Common;

namespace ParticlesPlus
{
    public class ModConfig
    {
        public int Version { get; set; }
        public bool Global { get; set; } = true;
        public Dictionary<string, PresetConfig> Presets { get; set; } = new();
        public Dictionary<string, AdvancedParticleProperties[]> Particles { get; set; } = new();
    }

    public class PresetConfig
    {
        public bool Enabled { get; set; }
        public string Wildcard { get; set; }
        public string Particles { get; set; }
    }
}
