using System.Collections.Generic;
using Multiplayer.Common;

namespace Multiplayer.Client;

public record GameDataSnapshot(
    int CachedAtTime,
    byte[] GameData,
    byte[] SessionData,
    Dictionary<int, byte[]> MapData,
    Dictionary<int, List<ScheduledCommand>> MapCmds // Global cmds are -1, this is mutated by MultiplayerSession.ScheduleCommand
)
{
    // Every cmd of MapCmds in the order the server sent them, when known (join data of a server that sends it). The
    // game loaded from this snapshot runs them all from the world queue in this order, as live cmds run with
    // consistentCommandOrder (see Loader.PostLoad). Mutated by MultiplayerSession.ScheduleCommand like MapCmds.
    public List<ScheduledCommand>? OrderedCmds { get; init; }
}
