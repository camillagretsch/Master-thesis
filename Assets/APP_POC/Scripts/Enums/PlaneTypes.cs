using System;

/// <summary>
/// All possible plane types a surface plane can be. 
/// </summary>
[Flags]
public enum PlaneTypes
{
    Wall = 0x1,
    Floor = 0x2,
    Ceiling = 0x4,
    Table = 0x8,
    Unknown = 0x10
}
