using SpaceDodgeRL.library.encounter;
using SpaceDodgeRL.library.encounter.rulebook;
using SpaceDodgeRL.library.encounter.rulebook.actions;
using SpaceDodgeRL.scenes.encounter.state;
using SpaceDodgeRL.scenes.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpaceDodgeRL.scenes.components.AI {

  public class HastatusAIComponent : DeciderAIComponent {
    public static readonly string ENTITY_GROUP = "MARCHER_AI_COMPONENT_GROUP";
    public override string EntityGroup => ENTITY_GROUP;

    [JsonInclude] public int FormationNumber { get; private set; }
    [JsonInclude] public string UnitId { get; private set; }
    [JsonInclude] public int PilasRemaining { get; private set; }

    public int TestTimer { get; set; }

    public HastatusAIComponent(int formationNumber, string unitId) {
      this.FormationNumber = formationNumber;
      this.UnitId = unitId;
      this.PilasRemaining = 1;

      this.TestTimer = 0;
    }

    public static HastatusAIComponent Create(string saveData) {
      return JsonSerializer.Deserialize<HastatusAIComponent>(saveData);
    }

    private static EncounterPosition _DecideFormationPosition(int formationNumber, EncounterPosition center, Unit unit) {
      if (unit.UnitFormation == FormationType.MANIPULE_CLOSED) {
        return AIUtils.PositionInManipuleClosed(formationNumber, unit);
      } else {
        return AIUtils.PositionInManipuleOpened(formationNumber, unit);
      }
    }

    private List<EncounterAction> _ActionsForUnitReform(EncounterState state, Entity parent) {
      var actions = new List<EncounterAction>();

      var parentPos = parent.GetComponent<PositionComponent>().EncounterPosition;
      var playerPos = state.Player.GetComponent<PositionComponent>().EncounterPosition;

      var targetPosition = _DecideFormationPosition(this.FormationNumber, playerPos, state.GetUnit(this.UnitId));

      if (parentPos != targetPosition) {
        // TODO: We may want to make pathfinding stateful/cache it or something, to save on turn times
        var path = Pathfinder.AStarWithNewGrid(parentPos, targetPosition, state);
        if (path != null) {
          actions.Add(new MoveAction(parent.EntityId, path[0]));
        } else {
          actions.Add(new WaitAction(parent.EntityId));
        }
      } else {
        actions.Add(new WaitAction(parent.EntityId));
      }

      return actions;
    }

    private static List<Entity> HostilesInPosition(EncounterState state, FactionName parentFaction, int x, int y) {
      var hostiles = new List<Entity>();
      foreach (Entity e in state.EntitiesAtPosition(x, y)) {
        var factionComponent = e.GetComponent<FactionComponent>();
        if (factionComponent != null && factionComponent.Faction != parentFaction) {
          hostiles.Add(e);
        }
      }
      return hostiles;
    }

    private List<EncounterAction> _ActionsForUnitAdvance(EncounterState state, Entity parent) {
      var actions = new List<EncounterAction>();

      var parentPos = parent.GetComponent<PositionComponent>().EncounterPosition;
      var parentFaction = parent.GetComponent<FactionComponent>().Faction;

      // TODO: build a danger map?
      // TODO: throw OVER the heads of the engaged line
      if (this.PilasRemaining > 0) {
        for (int x = parentPos.X - 3; x <= parentPos.X + 3; x++) {
          for (int y = parentPos.Y - 3; y <= parentPos.Y + 3; y++) {
            var possibleHostiles = HostilesInPosition(state, parentFaction, x, y);
            if (possibleHostiles.Count > 0) {
              actions.Add(FireProjectileAction.CreatePilaAction(parent.EntityId, possibleHostiles[0]));
                this.PilasRemaining -= 1;
                return actions;
            }
          }
        }
      }

      var moveVec = AIUtils.Rotate(0, -1, state.GetUnit(this.UnitId).UnitFacing);
      var targetPos = new EncounterPosition(parentPos.X + moveVec.Item1, parentPos.Y + moveVec.Item2);
      if (state.EntitiesAtPosition(targetPos.X, targetPos.Y).Count == 0) {
        // Move
        actions.Add(new MoveAction(parent.EntityId, targetPos));
        // Attack TODO: replace this with charge
        var adjacentHostiles = new List<Entity>();
        foreach (var newpos in state.AdjacentPositions(targetPos)) {
          adjacentHostiles.AddRange(HostilesInPosition(state, parentFaction, newpos.X, newpos.Y));
        }
        // TODO: don't attack randomly
        if (adjacentHostiles.Count > 0) {
          var target = adjacentHostiles[state.EncounterRand.Next(adjacentHostiles.Count)];
          actions.Add(new MeleeAttackAction(parent.EntityId, target));
        }
      } else {
        var adjacentHostiles = new List<Entity>();
        foreach (var newpos in state.AdjacentPositions(parentPos)) {
          adjacentHostiles.AddRange(HostilesInPosition(state, parentFaction, newpos.X, newpos.Y));
        }
        // TODO: don't attack randomly
        if (adjacentHostiles.Count > 0) {
          var target = adjacentHostiles[state.EncounterRand.Next(adjacentHostiles.Count)];
          actions.Add(new MeleeAttackAction(parent.EntityId, target));
        } else {
          actions.Add(new WaitAction(parent.EntityId));
        }
      }
      
      return actions;
    }

    public override List<EncounterAction> _DecideNextAction(EncounterState state, Entity parent) {
      this.TestTimer += 1;
      if (this.TestTimer == 20 && this.FormationNumber == 0) {
        state.GetUnit(this.UnitId).StandingOrder = UnitOrder.ADVANCE;
      }
      if (this.TestTimer == 30 && this.FormationNumber == 0) {
        state.GetUnit(this.UnitId).UnitFormation = FormationType.MANIPULE_OPENED;
        state.GetUnit(this.UnitId).StandingOrder = UnitOrder.REFORM;
        state.GetUnit(this.UnitId).CenterPosition = parent.GetComponent<PositionComponent>().EncounterPosition;
      }
      if (this.TestTimer == 40 && this.FormationNumber == 0) {
        state.GetUnit(this.UnitId).StandingOrder = UnitOrder.ADVANCE;
      }

      var unit = state.GetUnit(this.UnitId);
      if (unit.StandingOrder == UnitOrder.REFORM) {
        return _ActionsForUnitReform(state, parent);
      } else if (unit.StandingOrder == UnitOrder.ADVANCE) {
        return _ActionsForUnitAdvance(state, parent);
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