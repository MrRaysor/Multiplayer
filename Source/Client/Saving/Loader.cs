using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using HarmonyLib;
using Multiplayer.Common;
using Verse;
using Verse.Profile;

namespace Multiplayer.Client.Saving;

public static class Loader
{
    public static void ReloadGame(List<int> mapsToLoad, bool changeScene, bool forceAsyncTime) => ReloadGame(mapsToLoad,
        changeScene, () =>
        {
            if (forceAsyncTime) Multiplayer.game.gameComp.asyncTime = true;
        });

    public static void ReloadGame(List<int> mapsToLoad, bool changeScene, Action customPostLoadAction)
    {
        var gameDoc = DataSnapshotToXml(Multiplayer.session.dataSnapshot, mapsToLoad);

        LoadPatch.gameToLoad = new TempGameData(gameDoc, Multiplayer.session.dataSnapshot.SessionData);
        TickPatch.replayTimeSpeed = TimeSpeed.Paused;

        if (changeScene)
        {
            LongEventHandler.QueueLongEvent(() =>
            {
                MemoryUtility.ClearAllMapsAndWorld();
                Current.Game = new Game
                {
                    InitData = new GameInitData
                    {
                        gameToLoad = "server"
                    }
                };

                LongEventHandler.ExecuteWhenFinished(() =>
                {
                    LongEventHandler.QueueLongEvent(() =>
                    {
                        PostLoad();
                        customPostLoadAction();
                    }, "MpSimulating", false, null);
                });
            }, "Play", "MpLoading", true, null);
        }
        else
        {
            LongEventHandler.QueueLongEvent(() =>
            {
                SaveLoad.LoadInMainThread(LoadPatch.gameToLoad);
                PostLoad();
                customPostLoadAction();
            }, "MpLoading", false, null);
        }
    }

    private static void PostLoad()
    {
        // If the client gets disconnected during loading
        if (Multiplayer.Client == null) return;

        var orderedCmds = Multiplayer.session.dataSnapshot.OrderedCmds;
        Multiplayer.session.dataSnapshot = Multiplayer.session.dataSnapshot with
        {
            CachedAtTime = TickPatch.Timer,
            OrderedCmds = null
        };

        Multiplayer.session.replayTimerStart = TickPatch.Timer;

        Multiplayer.game.ChangeRealPlayerFaction(
            Find.FactionManager.GetById(Multiplayer.session.myFactionId) ?? Multiplayer.WorldComp.spectatorFaction
        );
        Multiplayer.game.myFactionLoading = null;

        // In the server's order, every cmd runs from the world queue, as live cmds do with consistentCommandOrder: per map
        // queues run a tick's world cmds before its map cmds, and the cmds sent while the timer waited for a joining
        // player all share one tick (map queues stay empty then, see AsyncTimeComp.FinalizeInit)
        Multiplayer.AsyncWorldTime.cmds = new Queue<ScheduledCommand>(
            orderedCmds ?? Multiplayer.session.dataSnapshot.MapCmds.GetValueSafe(ScheduledCommand.Global) ?? []);
        // Otherwise map cmds are added in MapAsyncTimeComp.FinalizeInit
    }

    private static XmlDocument DataSnapshotToXml(GameDataSnapshot dataSnapshot, List<int> mapsToLoad)
    {
        XmlDocument gameDoc = ScribeUtil.LoadDocument(dataSnapshot.GameData);
        XmlNode gameNode = gameDoc.DocumentElement["game"];

        foreach (int map in mapsToLoad)
        {
            using XmlReader reader = XmlReader.Create(new MemoryStream(dataSnapshot.MapData[map]));
            XmlNode mapNode = gameDoc.ReadNode(reader);
            gameNode["maps"].AppendChild(mapNode);
        }

        return gameDoc;
    }
}
