using System;

namespace Cornifer.Structures
{
    [Flags]
    public enum RainWorldFeatures 
    {
        None = 0,
        Legacy = 1,
        Remix = 2,
        Downpour = 4,

        Steam = 8,

        All = Legacy | Remix | Downpour | Steam,
    }
}
