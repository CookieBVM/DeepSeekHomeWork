using UnityEngine;

namespace DeepSeek.DigitalHuman
{
    public static class DigitalHumanBootstrap
    {
        private static void CreateRuntimeController()
        {
            // Runtime auto-creation is intentionally disabled.
            // The digital human system now lives as regular scene UGUI in SampleScene
            // so every UI object can be edited manually from the Hierarchy/Inspector.
        }
    }
}
