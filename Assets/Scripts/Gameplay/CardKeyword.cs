using System;

[Flags]
public enum CardKeyword
{
    None = 0,
    Blitz = 1 << 0,
    Guard = 1 << 1,
    Fury = 1 << 2,
    Smokescreen = 1 << 3,
    Ambush = 1 << 4,
    Mobilize = 1 << 5,
    HeavyArmor = 1 << 6,
    Pinned = 1 << 7
}
