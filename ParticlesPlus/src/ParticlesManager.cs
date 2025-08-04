using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ParticlesPlus
{
    public class ParticlesManager
    {
        private readonly ICoreClientAPI _capi;
        private readonly ModConfig _modConfig;
        private readonly ParticlesManager _particlesManager;
        public ParticlesManager(ModSystem modSystem)
        {
            _capi = modSystem.capi;
            _modConfig = modSystem.modConfig;
        }
        private AdvancedParticleProperties[] GetParticles(string particlesKey)
        {
            return _modConfig.Particles.TryGetValue(particlesKey, out var particles)
                ? particles
                : Array.Empty<AdvancedParticleProperties>();
        }
        private Block[] GetBlocks(string wildcard)
        {
            return _capi.World.SearchBlocks(wildcard);
        }
        public void RemoveParticles(string wildcard)
        {
            Block[] blocks = GetBlocks(wildcard);
            foreach (Block block in blocks)
            {
                block.ParticleProperties = Array.Empty<AdvancedParticleProperties>();
            }
        }
        public void AddParticles(string wildcard, string particlesKey)
        {
            AdvancedParticleProperties[] particles = GetParticles(particlesKey);
            if (particles == null || particles.Length == 0) return;

            Block[] blocks = GetBlocks(wildcard);
            foreach (Block block in blocks)
            {
                block.ParticleProperties ??= Array.Empty<AdvancedParticleProperties>();
                block.ParticleProperties = block.ParticleProperties
                                .Concat(particles)
                                .ToArray();
            }
        }
        public void ApplyEnabledParticles()
        {
            foreach (var preset in _modConfig.Presets)
            {
                if (!preset.Value.Enabled) continue;

                AddParticles(preset.Value.Wildcard, preset.Value.Particles);
            }
        }
        public void RemoveEnabledParticles()
        {
            foreach (var preset in _modConfig.Presets)
            {
                if (!preset.Value.Enabled) continue;

                RemoveParticles(preset.Value.Wildcard);
            }
        }
    }
}
