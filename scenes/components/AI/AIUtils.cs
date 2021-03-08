using System;
using System.Collections.Generic;
using Godot;
using SpaceDodgeRL.library.encounter;
using SpaceDodgeRL.library.encounter.rulebook;
using SpaceDodgeRL.library.encounter.rulebook.actions;
using SpaceDodgeRL.scenes.encounter.state;
using SpaceDodgeRL.scenes.entities;

namespace SpaceDodgeRL.scenes.components.AI {
  public enum Flank {
    LEFT,
    RIGHT
  }

  public abstract class Formation {
    public abstract EncounterPosition PositionInFormation(int formationNumber, Unit unit);
    public abstract bool IsOnFlank(int formationNumber, Unit unit, Flank flank);
  }

  public class FormationManipuleClosed : Formation {

    public override EncounterPosition PositionInFormation(int formationNumber, Unit unit) {
      EncounterPosition center = unit.RallyPoint;
      
      int dx = formationNumber % 10;
      int dy = Mathf.FloorToInt(formationNumber / 10) - 1;
      Tuple<int, int> rotated = AIUtils.RotateForFormation(dx, dy, unit.UnitFacing);
      return new EncounterPosition(center.X + rotated.Item1, center.Y + rotated.Item2);
    }

    public override bool IsOnFlank(int formationNumber, Unit unit, Flank flank) {
      if (flank == Flank.LEFT) {
        return formationNumber % 10 == 0;
      } else if (flank == Flank.RIGHT) {
        return formationNumber % 10 == 9;
      } else {
        throw new NotImplementedException();
      }
    }
  }

  public class FormationManipuleOpened : Formation {
    
    public override EncounterPosition PositionInFormation(int formationNumber, Unit unit) {
      EncounterPosition center = unit.RallyPoint;
      int halfFormation = unit.NumInFormation / 2 + 1;

      if (formationNumber < halfFormation) {
        int dx = formationNumber % 10;
        int dy = Mathf.FloorToInt(formationNumber / 10) - 1;
        var rotated = AIUtils.RotateForFormation(dx, dy, unit.UnitFacing);
        return new EncounterPosition(center.X + rotated.Item1, center.Y + rotated.Item2);
      } else {
        int dx = formationNumber % 10 - 10;
        int dy = Mathf.FloorToInt((formationNumber - halfFormation) / 10) - 1;
        var rotated = AIUtils.RotateForFormation(dx, dy, unit.UnitFacing);
        return new EncounterPosition(center.X + rotated.Item1, center.Y + rotated.Item2);
      }
    }

    public override bool IsOnFlank(int formationNumber, Unit unit, Flank flank) {
      int halfFormation = unit.NumInFormation / 2 + 1;
      if (flank == Flank.LEFT) {
        return formationNumber > halfFormation && formationNumber % 10 == 0;
      } else if (flank == Flank.RIGHT) {
        return formationNumber < halfFormation && formationNumber % 10 == 9;
      } else {
        throw new NotImplementedException();
      }
    }
  }

  public class FormationLine20 : Formation {
    public override EncounterPosition PositionInFormation(int formationNumber, Unit unit) {
      EncounterPosition center = unit.RallyPoint;
      
      int dx = formationNumber % 20 - 10;
      int dy = Mathf.FloorToInt(formationNumber / 20) - 1;
      Tuple<int, int> rotated = AIUtils.RotateForFormation(dx, dy, unit.UnitFacing);
      return new EncounterPosition(center.X + rotated.Item1, center.Y + rotated.Item2);
    }

    public override bool IsOnFlank(int formationNumber, Unit unit, Flank flank) {
      if (flank == Flank.LEFT) {
        return formationNumber % 20 == 0;
      } else if (flank == Flank.RIGHT) {
        return formationNumber % 20 == 9;
      } else {
        throw new NotImplementedException();
      }
    }
  }

  public static class AIUtils {

    public static Dictionary<FormationType, Formation> FormationDictionary = new Dictionary<FormationType, Formation>() {
      { FormationType.MANIPULE_CLOSED, new FormationManipuleClosed() },
      { FormationType.MANIPULE_OPENED, new FormationManipuleOpened() },
      { FormationType.LINE_20, new FormationLine20() }
    };

    public static EncounterPosition RotateAndProject(EncounterPosition origin, int x, int y, FormationFacing facing) {
      var vec = Rotate(x, y, facing);
      return new EncounterPosition(origin.X + vec.Item1, origin.Y + vec.Item2);
    }

    public static Tuple<int, int> RotateForFormation(int x, int y, FormationFacing facing) {
      if (facing == FormationFacing.NORTH) {
        return new Tuple<int, int>(x, y);
      } else if (facing == FormationFacing.EAST) {
        return new Tuple<int, int>(-y, x);
      } else if (facing == FormationFacing.SOUTH) {
        return new Tuple<int, int>(-x - 1, -y);
      } else if (facing == FormationFacing.WEST) {
        return new Tuple<int, int>(y, -x - 1);
      } else {
        throw new NotImplementedException();
      }
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

    private static EncounterPosition _DecideFormationPosition(int formationNumber, EncounterPosition center, Unit unit) {
      return AIUtils.FormationDictionary[unit.UnitFormation].PositionInFormation(formationNumber, unit);
    }

    // TODO: A more accurate algorithm than "is your position on the flank"
    public static bool IsOnFlank(int formationNumber, Unit unit, Flank flank) {
      return AIUtils.FormationDictionary[unit.UnitFormation].IsOnFlank(formationNumber, unit, flank);
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