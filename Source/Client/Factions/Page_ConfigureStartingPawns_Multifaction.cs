using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Multiplayer.Client.Factions
{
    internal class Page_ConfigureStartingPawns_Multifaction : Page_ConfigureStartingPawns
    {
        Faction _temporaryFactionForPawnCreation;
        // Without Ideology, FactionGenerator gives the temporary faction a new ideo and adds it to the IdeoManager: on
        // this player alone. Removed with the faction (CleanUpTemporaryFaction).
        readonly List<Ideo> _temporaryIdeos = new();
        Scenario _scenario;

        public override void PreOpen()
        {
            base.PreOpen();

            _scenario = Find.Scenario;

            GenerateTemporaryFaction();
            GeneratePawnsForConfigurePage();
        }

        public override void PostClose()
        {
            base.PostClose();

            CleanUpTemporaryFaction();
            FactionSidebar.ConfigurationPageClosed(this);
        }

        public override void DoNext()
        {
            CleanUpTemporaryFaction();

            base.DoNext();
        }

        private void CleanUpTemporaryFaction()
        {
            if (_temporaryFactionForPawnCreation == null) return;

            Find.FactionManager.Remove(_temporaryFactionForPawnCreation);
            Find.FactionManager.toRemove.Remove(_temporaryFactionForPawnCreation);
            // FactionAddPatch registered it: a reference to it would resolve on this player alone. (No Multiplayer game
            // when the page closes after a disconnection.)
            if (Multiplayer.game != null)
                ScribeUtil.sharedCrossRefs.Unregister(_temporaryFactionForPawnCreation);

            // Its ideos exist on this player alone. Left in the IdeoManager (and queued in its toRemove by
            // FactionManager.Remove), this player's list of ideos differs from the other players'. The starting pawns
            // sent by SendPawn reference the temporary ideo, which no player can resolve, so on loading them every
            // player gives them a random ideo of its IdeoManager (Pawn_IdeoTracker.ExposeData): another one on the
            // creator, and the new colony started differently there (recreation needs, desync, 2026-11-07).
            foreach (var ideo in _temporaryIdeos)
            {
                Find.IdeoManager.ideos.Remove(ideo);
                Find.IdeoManager.toRemove.Remove(ideo);
            }
            _temporaryIdeos.Clear();

            Current.Game.InitData.playerFaction = null;
            _temporaryFactionForPawnCreation = null;
        }

        private void GenerateTemporaryFaction()
        {
            var ideosBefore = new HashSet<Ideo>(Find.IdeoManager.IdeosListForReading);
            _temporaryFactionForPawnCreation = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(_scenario.playerFaction.factionDef));
            _temporaryFactionForPawnCreation.temporary = true;
            _temporaryIdeos.AddRange(Find.IdeoManager.IdeosListForReading.Where(i => !ideosBefore.Contains(i)));

            Find.FactionManager.Add(_temporaryFactionForPawnCreation);
        }

        private void GeneratePawnsForConfigurePage()
        {
            var prevProgramState = Current.programStateInt;
            SetEntryProgramStateForCorrectPawnCreation();

            Current.Game.InitData.playerFaction = _temporaryFactionForPawnCreation;

            try
            {
                GeneratingPawns();
            }
            finally
            {       
                Current.programStateInt = prevProgramState;
            }
        }

        private static void SetEntryProgramStateForCorrectPawnCreation()
        {
            // Set ProgramState.Entry to ensure InInterface is false.
            // (#508 Tick-git)  InInterface seems unrelated to PawnCreation to me,
            //                  but it's necessary because it makes Faction.OfPlayer return
            //                  the temporary generated faction — which is required for proper apparel generation.

            Current.programStateInt = ProgramState.Entry;
        }

        private void GeneratingPawns()
        {
            _scenario.PostIdeoChosen();
        }
    }
}
