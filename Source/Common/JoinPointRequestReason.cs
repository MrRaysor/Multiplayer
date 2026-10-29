namespace Multiplayer.Common;

public enum JoinPointRequestReason : byte
{
    Save = 1,
    WorldTravel = 2,
    // A player saves the game: the file is written from the join point, made by every player at the same tick.
    SaveGame = 3,
}
