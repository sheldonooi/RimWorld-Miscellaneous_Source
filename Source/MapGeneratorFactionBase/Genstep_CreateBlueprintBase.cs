﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Verse;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using Verse.AI;
using Verse.AI.Group;

namespace MapGenerator
{
    public class Genstep_CreateBlueprintBase : GenStep_Scatterer
    {
        public bool testActive = false;

        public bool randomlyUseVanilla = true;

        private ThingDef selectedWallStuff;

        private static Dictionary<int, string> mapWorldCoord2Blueprint;

        protected override bool CanScatterAt(IntVec3 c, Map map)
        {
            return base.CanScatterAt(c, map) && c.Standable(map) && !c.Roofed(map) && map.reachability.CanReachMapEdge(c, TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false));
        }


        protected override void ScatterAt(IntVec3 c, Map map, int stackCount = 1)
        {
            if (testActive)
                Log.Warning("Genstep_CreateBlueprintBase - Test-Mode is active!");

            Faction faction;
            if (map.info.parent == null || map.info.parent.Faction == null || map.info.parent.Faction == Faction.OfPlayer)
            {
                faction = Find.FactionManager.RandomEnemyFaction(false, false, false);
            }
            else
            {
                faction = map.info.parent.Faction;
            }

            int worldTile = -1;
            if (map != null)
                worldTile = map.Tile;

            TechLevel techlevel = faction.def.techLevel;
            // Select only blueprints with a techlevel corresponding to the faction techlevel
            IEnumerable<MapGeneratorBaseBlueprintDef> blueprint1stSelection = DefDatabase<MapGeneratorBaseBlueprintDef>.AllDefsListForReading
                .Where((MapGeneratorBaseBlueprintDef b) => b.techLevelRequired <= techlevel && (b.techLevelMax == TechLevel.Undefined || b.techLevelMax >= techlevel) );

            float createVanillaLimit = 0.95f;
            if ( blueprint1stSelection != null && blueprint1stSelection.Count() > 0 )
            {
                if (blueprint1stSelection.Count() <= 3)
                    createVanillaLimit = 0.85f;
                else if (blueprint1stSelection.Count() <= 5)
                    createVanillaLimit = 0.80f;
                else if (blueprint1stSelection.Count() <= 7)
                    createVanillaLimit = 0.70f;
                else if (blueprint1stSelection.Count() <= 10)
                    createVanillaLimit = 0.65f;
                else if (blueprint1stSelection.Count() <= 15)
                    createVanillaLimit = 0.50f;
                else if (blueprint1stSelection.Count() <= 20)
                    createVanillaLimit = 0.45f;
                else
                    createVanillaLimit = 0.35f;
            }

            if ( blueprint1stSelection == null || blueprint1stSelection.Count() == 0 )
                Log.Warning("Genstep_CreateBlueprintBase - no usable blueprint found. Using vanilla base generation..");


            if ( blueprint1stSelection == null || blueprint1stSelection.Count() == 0 || 
                ( !testActive && randomlyUseVanilla && Rand.Value < createVanillaLimit && 
                  (mapWorldCoord2Blueprint == null || !mapWorldCoord2Blueprint.ContainsKey(worldTile))
                ))
            {
                // No blueprint for this faction techlevel found?
                // Use basic base builder code instead!
                Core_ScatterAt(c, map, stackCount);
                return;
            }

            MapGeneratorBaseBlueprintDef blueprint = blueprint1stSelection.RandomElementByWeight((MapGeneratorBaseBlueprintDef b) => b.chance);

            // Check if this position was already used -> re-use old blueprint 
            if (mapWorldCoord2Blueprint == null)
                mapWorldCoord2Blueprint = new Dictionary<int, string>();
            if (mapWorldCoord2Blueprint.ContainsKey(worldTile))
            {
                MapGeneratorBaseBlueprintDef newBlueprint = DefDatabase<MapGeneratorBaseBlueprintDef>.GetNamedSilentFail(mapWorldCoord2Blueprint[worldTile]);
                if (newBlueprint != null && newBlueprint.techLevelRequired <= faction.def.techLevel && newBlueprint.techLevelMax >= faction.def.techLevel)
                    blueprint = newBlueprint;
            }
            else if (worldTile != -1)
                mapWorldCoord2Blueprint.Add(worldTile, blueprint.defName);

            // place the blueprint
            BlueprintHandler.CreateBlueprintAt(c, map, blueprint, faction, ref selectedWallStuff, ref usedSpots);

            // reset
            selectedWallStuff = null;

            // Add a message to honor the creator
            if (!blueprint.createdBy.NullOrEmpty()) {
                string label = "MapGenerator_FactionBase_Header_ProvidedBy".Translate();
                string text = "MapGenerator_FactionBase_Body_ProvidedBy".Translate(new object[] { blueprint.createdBy });
                Find.LetterStack.ReceiveLetter(label, text, LetterDefOf.PositiveEvent, new GlobalTargetInfo(c, map));
            }
        }








        // This is core code, that will be used if there isn't a blueprint available for the required tech level of the faction
        // Original from RimWorld.GenStep_FactionBase 

        private static readonly IntRange FactionBaseSizeRange = new IntRange(22, 23);

        protected void Core_ScatterAt(IntVec3 c, Map map, int stackCount = 1)
        {

            GenStep_FactionBase gs = new GenStep_FactionBase();
            //gs.ForceScatterAt(c, map);
            gs.ReflectCall("ScatterAt", c, map, stackCount);
            
            return;



            //int randomInRange = FactionBaseSizeRange.RandomInRange; // modified!
            //int randomInRange2 = FactionBaseSizeRange.RandomInRange; // modified!

            //CellRect rect = new CellRect(c.x - randomInRange / 2, c.z - randomInRange2 / 2, randomInRange, randomInRange2);
            //Faction faction;
            //if (map.info.parent == null || map.info.parent.Faction == null || map.info.parent.Faction == Faction.OfPlayer)
            //{
            //    faction = Find.FactionManager.RandomEnemyFaction(false, false);
            //}
            //else
            //{
            //    faction = map.info.parent.Faction;
            //}
            //if (FactionBaseSymbolResolverUtility.ShouldUseSandbags(faction))
            //{
            //    rect = rect.ExpandedBy(4);
            //}
            //rect.ClipInsideMap(map);
            //ResolveParams resolveParams = default(ResolveParams);
            //resolveParams.rect = rect;
            //resolveParams.faction = faction;
            //BaseGen.globalSettings.map = map;
            //BaseGen.symbolStack.Push("factionBase", resolveParams);
            //BaseGen.Generate();

        }


    }
}
