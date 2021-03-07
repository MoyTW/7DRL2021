using Godot;
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

  public class ManipularAIComponent : DeciderAIComponent {
    public static readonly string ENTITY_GROUP = "MARCHER_AI_COMPONENT_GROUP";
    public override string EntityGroup => ENTITY_GROUP;

    [JsonInclude] public int FormationNumber { get; private set; }
    [JsonInclude] public string UnitId { get; private set; }

    public ManipularAIComponent(int formationNumber, string unitId) {
      this.FormationNumber = formationNumber;
      this.UnitId = unitId;
    }

    public static ManipularAIComponent Create(string saveData) {
      return JsonSerializer.Deserialize<ManipularAIComponent>(saveData);
    }

    private static Tuple<int, int> Rotate(int x, int y, FormationFacing facing) {
      if (facing == FormationFacing.NORTH) {
        return new Tuple<int, int>(x, y);
      } else if (facing == FormationFacing.EAST) {
        return new Tuple<int, int>(-y, x);
      } else if (facing == FormationFacing.SOUTH) {
        return new Tuple<int, int>(-x, -y);
      } else if (facing == FormationFacing.WEST) {
        return new Tuple<int, int>(y, -x);
      } else {
        throw new NotImplementedException();
      }
    }

    private static EncounterPosition PositionInManipuleClosed(int formationNumber, Unit unit) {
      EncounterPosition center = unit.CenterPosition;
      
      int dx = formationNumber % 10;
      int dy = Mathf.FloorToInt(formationNumber / 10) - 1;
      Tuple<int, int> rotated = Rotate(dx, dy, unit.UnitFacing);
      return new EncounterPosition(center.X + rotated.Item1, center.Y + rotated.Item2);
    }

    private static EncounterPosition PositionInManipuleOpened(int formationNumber, Unit unit) {
      int numInFormation = unit.BattleReadyEntities.Count;
      EncounterPosition center = unit.CenterPosition;
      int halfFormation = numInFormation / 2 + 1;

      if (formationNumber < halfFormation) {
        int dx = formationNumber % 10;
        int dy = Mathf.FloorToInt(formationNumber / 10) - 1;
        var rotated = Rotate(dx, dy, unit.UnitFacing);
        return new EncounterPosition(center.X + rotated.Item1, center.Y + rotated.Item2);
      } else {
        int dx = formationNumber % 10 - 10;
        int dy = Mathf.FloorToInt((formationNumber - halfFormation) / 10) - 1;
        var rotated = Rotate(dx, dy, unit.UnitFacing);
        return new EncounterPosition(center.X + rotated.Item1, center.Y + rotated.Item2);
      }
    }

    private static EncounterPosition _DecideFormationPosition(int formationNumber, EncounterPosition center, Unit unit) {
      if (unit.UnitFormation == FormationType.MANIPULE_CLOSED) {
        return PositionInManipuleClosed(formationNumber, unit);
      } else {
        return PositionInManipuleOpened(formationNumber, unit);
      }
    }

    public override List<EncounterAction> _DecideNextAction(EncounterState state, Entity parent) {
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

    public override string Save() {
      return JsonSerializer.Serialize(this);
    }

    public override void NotifyAttached(Entity parent) { }

    public override void NotifyDetached(Entity parent) { }
  }
}