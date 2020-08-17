using Stride.Engine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Zerobot.Player
{
    /// <summary>
    /// Represents the data contained in a Marker instance
    /// </summary>
    public struct ZerobotMarker
    {
        public List<Entity> StartEffectPrefabInstance;

        public List<Entity> EndEffectPrefabInstance;

        public List<Entity> Trail;
    }
}
