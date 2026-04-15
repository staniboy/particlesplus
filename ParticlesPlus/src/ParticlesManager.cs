using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ParticlesPlus
{
    public class ParticlesManager(ModSystem modSystem)
    {
        private ICoreClientAPI API => modSystem.API;

        private Block[] GetBlocks(string wildcard)
        {
            return API.World.SearchBlocks(wildcard);
        }
        public void RemoveParticles(string wildcard)
        {
            Block[] blocks = GetBlocks(wildcard);
            foreach (Block block in blocks)
            {
                block.ParticleProperties = Array.Empty<AdvancedParticleProperties>();
            }
        }
        public void AddParticles(string wildcard, AdvancedParticleProperties[] particles)
        {
            if (particles == null || particles.Length == 0) return;
            if (string.IsNullOrEmpty(wildcard)) return;

            Block[] blocks = GetBlocks(wildcard);
            foreach (Block block in blocks)
            {
                block.ParticleProperties ??= Array.Empty<AdvancedParticleProperties>();
                block.ParticleProperties = block.ParticleProperties
                                .Concat(particles)
                                .ToArray();
            }
        }
    }
}
