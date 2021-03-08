using System;
using System.Collections.Generic;
using Godot;
using SpaceDodgeRL.library.encounter;
using SpaceDodgeRL.library.encounter.rulebook;
using SpaceDodgeRL.library.encounter.rulebook.actions;
using SpaceDodgeRL.scenes.encounter.state;
using SpaceDodgeRL.scenes.entities;

namespace SpaceDodgeRL.scenes.components.AI {
  public static class AIUtils {

    public static EncounterPosition RotateAndProject(EncounterPosition origin, int x, int y, FormationFacing facing) {
      var vec = Rotate(x, y, facing);
      return new EncounterPosition(origin.X + vec.Item1, origin.Y + vec.Item2);
    }

    public static Tuple<int, int> Rotate(int x, int y, FormationFacing facing) {
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

    public static List<Entity> HostilesInPosition(EncounterState state, FactionName parentFaction, int x, int y) {
      var hostiles = new List<Entity>();
      foreach (Entity e in state.EntitiesAtPosition(x, y)) {
        var factionComponent = e.GetComponent<FactionComponent>();
        if (factionComponent != null && factionComponent.Faction != parentFaction) {
          hostiles.Add(e);
        }
      }
      return hostiles;
    }

    public static List<Entity> AdjacentHostiles(EncounterState state, FactionName parentFaction, EncounterPosition position) {
      var adjacentHostiles = new List<Entity>();
      foreach (var newpos in state.AdjacentPositions(position)) {
        adjacentHostiles.AddRange(AIUtils.HostilesInPosition(state, parentFaction, newpos.X, newpos.Y));
      }
      return adjacentHostiles;
    }

    private static EncounterPosition _PositionInManipuleClosed(int formationNumber, Unit unit) {
      EncounterPosition center = unit.RallyPoint;
      
      int dx = formationNumber % 10;
      int dy = Mathf.FloorToInt(formationNumber / 10) - 1;
      Tuple<int, int> rotated = AIUtils.Rotate(dx, dy, unit.UnitFacing);
      return new EncounterPosition(center.X + rotated.Item1, center.Y + rotated.Item2);
    }

    private static EncounterPosition _PositionInManipuleOpened(int formationNumber, Unit unit) {
      int numInFormation = unit.BattleReadyEntities.Count;
      EncounterPosition center = unit.RallyPoint;
      int halfFormation = numInFormation / 2 + 1;

      if (formationNumber < halfFormation) {
        int dx = formationNumber % 10;
        int dy = Mathf.FloorToInt(formationNumber / 10) - 1;
        var rotated = AIUtils.Rotate(dx, dy, unit.UnitFacing);
        return new EncounterPosition(center.X + rotated.Item1, center.Y + rotated.Item2);
      } else {
        int dx = formationNumber % 10 - 10;
        int dy = Mathf.FloorToInt((formationNumber - halfFormation) / 10) - 1;
        var rotated = AIUtils.Rotate(dx, dy, unit.UnitFacing);
        return new EncounterPosition(center.X + rotated.Item1, center.Y + rotated.Item2);
      }
    }

    private static EncounterPosition _PositionInLine20(int formationNumber, Unit unit) {
      EncounterPosition center = unit.RallyPoint;
      
      int dx = formationNumber % 20 - 10;
      int dy = Mathf.FloorToInt(formationNumber / 20) - 1;
      Tuple<int, int> rotated = AIUtils.Rotate(dx, dy, unit.UnitFacing);
      return new EncounterPosition(center.X + rotated.Item1, center.Y + rotated.Item2);
    }

    private static EncounterPosition _DecideFormationPosition(int formationNumber, EncounterPosition center, Unit unit) {
      if (unit.UnitFormation == FormationType.MANIPULE_CLOSED) {
        return AIUtils._PositionInManipuleClosed(formationNumber, unit);
      } else if (unit.UnitFormation == FormationType.MANIPULE_OPENED) {
        return AIUtils._PositionInManipuleOpened(formationNumber, unit);
      } else if (unit.UnitFormation == FormationType.LINE_20) {
        return AIUtils._PositionInLine20(formationNumber, unit);
      } else {
        throw new NotImplementedException();
      }
    }

    public static List<EncounterAction> ActionsForUnitReform(EncounterState state, Entity parent, int formationNumber, Unit unit) {
      var actions = new List<EncounterAction>();

      var parentPos = parent.GetComponent<PositionComponent>().EncounterPosition;
      var playerPos = state.Player.GetComponent<PositionComponent>().EncounterPosition;

      var targetPosition = AIUtils._DecideFormationPosition(formationNumber, playerPos, unit);

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
  }
}