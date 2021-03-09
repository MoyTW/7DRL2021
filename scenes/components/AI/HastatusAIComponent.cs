using SpaceDodgeRL.library;
using SpaceDodgeRL.library.encounter;
using SpaceDodgeRL.library.encounter.rulebook;
using SpaceDodgeRL.library.encounter.rulebook.actions;
using SpaceDodgeRL.scenes.encounter.state;
using SpaceDodgeRL.scenes.entities;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpaceDodgeRL.scenes.components.AI {

  public class HastatusAIComponent : DeciderAIComponent {
    public static readonly string ENTITY_GROUP = "HASTATUS_AI_COMPONENT_GROUP";
    public override string EntityGroup => ENTITY_GROUP;

    [JsonInclude] public int FormationNumber { get; private set; }
    [JsonInclude] public int PilasRemaining { get; private set; }

    public HastatusAIComponent(int formationNumber) {
      this.FormationNumber = formationNumber;
      this.PilasRemaining = 1;
    }

    public static HastatusAIComponent Create(string saveData) {
      return JsonSerializer.Deserialize<HastatusAIComponent>(saveData);
    }

    private List<EncounterAction> _ActionsForUnitAdvance(EncounterState state, Entity parent, Unit unit) {
      var actions = new List<EncounterAction>();

      var parentPos = parent.GetComponent<PositionComponent>().EncounterPosition;
      var parentFaction = parent.GetComponent<FactionComponent>().Faction;

      // TODO: build a danger map?
      // TODO: throw OVER the heads of the engaged line
      if (this.PilasRemaining > 0) {
        for (int x = parentPos.X - 3; x <= parentPos.X + 3; x++) {
          for (int y = parentPos.Y - 3; y <= parentPos.Y + 3; y++) {
            var possibleHostiles = AIUtils.HostilesInPosition(state, parentFaction, x, y);
            if (possibleHostiles.Count > 0) {
              actions.Add(FireProjectileAction.CreatePilaAction(parent.EntityId, possibleHostiles[0]));
                this.PilasRemaining -= 1;
                return actions;
            }
          }
        }
      }

      var targetEndPos = parentPos;
      // We're gonna have some serious Phalanx Drift goin' on I guess?
      var forwardPositions = new List<EncounterPosition>() { AIUtils.RotateAndProject(parentPos, 0, -1, unit.UnitFacing) };
      if (!(unit.RightFlank && AIUtils.IsOnFlank(this.FormationNumber, unit, Flank.RIGHT))) {
        forwardPositions.Add(AIUtils.RotateAndProject(parentPos, 1, -1, unit.UnitFacing));
      }
      if (!(unit.LeftFlank && AIUtils.IsOnFlank(this.FormationNumber, unit, Flank.LEFT))) {
        forwardPositions.Add(AIUtils.RotateAndProject(parentPos, -1, -1, unit.UnitFacing));
      }
      
      foreach (var forwardPos in forwardPositions) {
        if (state.EntitiesAtPosition(forwardPos.X, forwardPos.Y).Count == 0) {
          // Never go into a square unless it's adjacent to an existing friendly
          if (AIUtils.AdjacentFriendlies(state, parent, parentFaction, forwardPos).Count > 0) {
            targetEndPos = forwardPos;
            break;
          }
        }
      }
      if (targetEndPos != parentPos) {
        actions.Add(new MoveAction(parent.EntityId, targetEndPos));
      }
      var adjacentHostiles = AIUtils.AdjacentHostiles(state, parentFaction, targetEndPos);
      if (adjacentHostiles.Count > 0) {
        // TODO: don't attack randomly
        var target = adjacentHostiles[state.EncounterRand.Next(adjacentHostiles.Count)];
        actions.Add(new MeleeAttackAction(parent.EntityId, target));
      }
      if (actions.Count == 0) {
        actions.Add(new WaitAction(parent.EntityId));
      }
      
      return actions;
    }

    // This is actually much more like unit broken but oh well!
    public static List<EncounterAction> ActionsForUnitRetreat(EncounterState state, Entity parent, Unit unit) {
      var parentPos = parent.GetComponent<PositionComponent>().EncounterPosition;

      EncounterPosition targetEndPos = parentPos;
      var backwardsPositions = new List<EncounterPosition>() {
        AIUtils.RotateAndProject(parentPos, 0, 1, unit.UnitFacing),
        AIUtils.RotateAndProject(parentPos, 1, 1, unit.UnitFacing),
        AIUtils.RotateAndProject(parentPos, -1, 1, unit.UnitFacing),
      };
      GameUtils.Shuffle(state.EncounterRand, backwardsPositions);
      foreach (var position in backwardsPositions) {
        if (state.EntitiesAtPosition(position.X, position.Y).Count == 0) {
          targetEndPos = position;
          break;
        }
      }
      if (targetEndPos == parentPos) {
        targetEndPos = backwardsPositions[0];
      }
      return new List<EncounterAction>() { new MoveAction(parent.EntityId, targetEndPos) };
    }

    public override List<EncounterAction> _DecideNextAction(EncounterState state, Entity parent) {
      var unit = state.GetUnit(parent.GetComponent<UnitComponent>().UnitId);

      if (unit.StandingOrder == UnitOrder.REFORM) {
        return AIUtils.ActionsForUnitReform(state, parent, this.FormationNumber, unit);
      } else if (unit.StandingOrder == UnitOrder.ADVANCE) {
        return _ActionsForUnitAdvance(state, parent, unit);
      } else if (unit.StandingOrder == UnitOrder.RETREAT) {
        return ActionsForUnitRetreat(state, parent, unit);
      } else {
        throw new NotImplementedException();
      }
    }

    public override string Save() {
      return JsonSerializer.Serialize(this);
    }

    public override void NotifyAttached(Entity parent) { }

    public override void NotifyDetached(Entity parent) { }
  }
}