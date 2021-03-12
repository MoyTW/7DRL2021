using System;
using System.Collections.Generic;
using Godot;
using SpaceDodgeRL.library;
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
    public abstract int Depth(int numInFormation);
    public abstract EncounterPosition PositionInFormation(int formationNumber, Unit unit);
    public abstract bool IsOnFlank(int x, int y, Unit unit, Flank flank);
  }

  public class FormationManipuleClosed : Formation {

    public override int Depth(int numInFormation) {
      return Mathf.CeilToInt(numInFormation / 10);
    }

    public override EncounterPosition PositionInFormation(int formationNumber, Unit unit) {
      EncounterPosition center = unit.RallyPoint;
      
      int dx = formationNumber % 10;
      int dy = Mathf.FloorToInt(formationNumber / 10) - 1;
      Tuple<int, int> rotated = AIUtils.RotateForFormation(dx, dy, unit.UnitFacing);
      return new EncounterPosition(center.X + rotated.Item1, center.Y + rotated.Item2);
    }

    public override bool IsOnFlank(int x, int y, Unit unit, Flank flank) {
      var northFacing = AIUtils.VectorFromCenterRotated(unit.RallyPoint, x, y, unit.UnitFacing);
      if (flank == Flank.LEFT) {
        return northFacing.Item1 <= -10;
      } else if (flank == Flank.RIGHT) {
        return northFacing.Item1 >= 9;
      } else {
        throw new NotImplementedException();
      }
    }
  }

  public class FormationManipuleOpened : Formation {

    public override int Depth(int numInFormation) {
      return Mathf.CeilToInt(numInFormation / 20);
    }

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

    public override bool IsOnFlank(int x, int y, Unit unit, Flank flank) {
      var northFacing = AIUtils.VectorFromCenterRotated(unit.RallyPoint, x, y, unit.UnitFacing);
      if (flank == Flank.LEFT) {
        return northFacing.Item1 <= -10;
      } else if (flank == Flank.RIGHT) {
        return northFacing.Item1 >= 9;
      } else {
        throw new NotImplementedException();
      }
    }

    
  }

  public class FormationLine20 : Formation {

    public override int Depth(int numInFormation) {
      return Mathf.CeilToInt(numInFormation / 20);
    }

    public override EncounterPosition PositionInFormation(int formationNumber, Unit unit) {
      EncounterPosition center = unit.RallyPoint;
      
      int dx = formationNumber % 20 - 10;
      int dy = Mathf.FloorToInt(formationNumber / 20) - 1;
      Tuple<int, int> rotated = AIUtils.RotateForFormation(dx, dy, unit.UnitFacing);
      return new EncounterPosition(center.X + rotated.Item1, center.Y + rotated.Item2);
    }

    public override bool IsOnFlank(int x, int y, Unit unit, Flank flank) {
      var northFacing = AIUtils.VectorFromCenterRotated(unit.RallyPoint, x, y, unit.UnitFacing);
      if (flank == Flank.LEFT) {
        return northFacing.Item1 <= -10;
      } else if (flank == Flank.RIGHT) {
        return northFacing.Item1 >= 9;
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

    private static Tuple<int, int> ToNorthCoordinates(int dx, int dy, FormationFacing facing) {
      if (facing == FormationFacing.NORTH) {
        return new Tuple<int, int>(dx, dy);
      } else if (facing == FormationFacing.EAST) {
        return new Tuple<int, int>(dy, -dx);
      } else if (facing == FormationFacing.SOUTH) {
        return new Tuple<int, int>(-dx - 1, -dy);
      } else if (facing == FormationFacing.WEST) { // TODO: uh, is that...right. test out east/west alignment.
        return new Tuple<int, int>(-dy - 1, dx);
      } else {
        throw new NotImplementedException();
      }
    }

    public static Tuple<int, int> VectorFromCenterRotated(EncounterPosition center, int tx, int ty, FormationFacing facing) {
      int dx = tx - center.X;
      int dy = ty - center.Y;
      return ToNorthCoordinates(dx, dy, facing);
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

    public static List<Entity> FriendliesInPosition(EncounterState state, Entity entity, FactionName parentFaction, int x, int y) {
      var friendlies = new List<Entity>();
      foreach (Entity e in state.EntitiesAtPosition(x, y)) {
        if (e != entity) {
          var factionComponent = e.GetComponent<FactionComponent>();
          if (factionComponent != null && factionComponent.Faction == parentFaction) {
            friendlies.Add(e);
          }
        }
      }
      return friendlies;
    }
    
    public static List<Entity> AdjacentFriendlies(EncounterState state, Entity entity, FactionName parentFaction, EncounterPosition position) {
      var adjacentFriendlies = new List<Entity>();
      foreach (var newpos in state.AdjacentPositions(position)) {
        adjacentFriendlies.AddRange(AIUtils.FriendliesInPosition(state, entity, parentFaction, newpos.X, newpos.Y));
      }
      return adjacentFriendlies;
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
    public static bool IsOnFlank(int x, int y, Unit unit, Flank flank) {
      return AIUtils.FormationDictionary[unit.UnitFormation].IsOnFlank(x, y, unit, flank);
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

    public static bool IsNextRowTooFarAhead(EncounterPosition parentPos, Unit unit) {
      var directlyInFrontPosition = AIUtils.RotateAndProject(parentPos, 0, -1, unit.UnitFacing);
      var forwardVec = AIUtils.VectorFromCenterRotated(unit.AveragePosition, directlyInFrontPosition.X, directlyInFrontPosition.Y, unit.UnitFacing);
      return forwardVec.Item2 < - Mathf.CeilToInt(unit.Depth / 2) - 1;
    }

    private static List<EncounterAction> ActionsUnitAdvanceFight(EncounterState state, Entity parent, Unit unit) {
      var actions = new List<EncounterAction>();

      var unitComponent = parent.GetComponent<UnitComponent>();
      var parentPos = parent.GetComponent<PositionComponent>().EncounterPosition;
      var parentFaction = parent.GetComponent<FactionComponent>().Faction;

      var targetEndPos = parentPos;
      bool tryAdvance = true;

      if (tryAdvance) {
        if (IsNextRowTooFarAhead(parentPos, unit)) {
          tryAdvance = false;
        }
      }

      // Morale check for advancing directly into an enemy
      if (tryAdvance) {
        var twoStepsAhead = AIUtils.RotateAndProject(parentPos, 0, -2, unit.UnitFacing);
        if (AIUtils.HostilesInPosition(state, parentFaction, twoStepsAhead.X, twoStepsAhead.Y).Count > 0) {
          var moraleState = parent.GetComponent<AIMoraleComponent>().CurrentMoraleState;
          if (moraleState == MoraleState.WAVERING) {
            if (state.EncounterRand.Next(2) == 0) {
              tryAdvance = false;
            }
          } else if (moraleState < MoraleState.WAVERING) {
            tryAdvance = false;
          }
        }
      }

      if (tryAdvance) {
        // We're gonna have some serious Phalanx Drift goin' on I guess?
        var forwardPositions = new List<EncounterPosition>() { AIUtils.RotateAndProject(parentPos, 0, -1, unit.UnitFacing) };
        if (!(unit.RightFlank && AIUtils.IsOnFlank(parentPos.X, parentPos.Y, unit, Flank.RIGHT))) {
          forwardPositions.Add(AIUtils.RotateAndProject(parentPos, 1, -1, unit.UnitFacing));
        }
        if (!(unit.LeftFlank && AIUtils.IsOnFlank(parentPos.X, parentPos.Y, unit, Flank.LEFT))) {
          forwardPositions.Add(AIUtils.RotateAndProject(parentPos, -1, -1, unit.UnitFacing));
        }

        foreach (var forwardPos in forwardPositions) {
          if (state.EntitiesAtPosition(forwardPos.X, forwardPos.Y).Count == 0) {
            // Never go into a square unless it's flanked by an existing friendly
            bool supported = false;
            for (int y = -1; y < 2; y++) {
              var leftPos = AIUtils.RotateAndProject(forwardPos, -1, y, unit.UnitFacing);
              if(AIUtils.FriendliesInPosition(state, parent, parentFaction, leftPos.X, leftPos.Y).Count > 0) {
                supported = true;
                break;
              }
              var rightPos = AIUtils.RotateAndProject(forwardPos, 1, y, unit.UnitFacing);
              if(AIUtils.FriendliesInPosition(state, parent, parentFaction, rightPos.X, rightPos.Y).Count > 0) {
                supported = true;
                break;
              }
            }
            if (supported) {
              targetEndPos = forwardPos;
              break;
            }
          }
        }
        if (targetEndPos != parentPos) {
          actions.Add(new MoveAction(parent.EntityId, targetEndPos));
        }
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

    private static List<EncounterAction> ActionsUnitAdvanceRotateOut(EncounterState state, Entity parent, Unit unit, bool backSecure) {
      var actions = new List<EncounterAction>();

      var unitComponent = parent.GetComponent<UnitComponent>();
      var parentPos = parent.GetComponent<PositionComponent>().EncounterPosition;
      var parentFaction = parent.GetComponent<FactionComponent>().Faction;
      var rotationComponent = parent.GetComponent<AIRotationComponent>();

      var positionBack = AIUtils.RotateAndProject(parentPos, 0, 1, unit.UnitFacing);

      var friendliesParent = AIUtils.FriendliesInPosition(state, parent, parentFaction, parentPos.X, parentPos.Y);
      bool parentPosSecure = false;
      foreach (var friendly in friendliesParent) {
        if (!friendly.IsRotating()) {
          parentPosSecure = true;
          break;
        }
      }

      // If nobody is in your square or behind you, hold position
      // Issue: if you have 2 units that are retreating, they'll form a self-reinforcing rout, which...I mean. lol.
      if (backSecure || parentPosSecure) {
        actions.Add(new MoveAction(parent.EntityId, positionBack));
      } else {
        rotationComponent.NotifyRotationCompleted();
        actions.Add(new WaitAction(parent.EntityId));
      }

      return actions;
    }

    public static List<EncounterAction> ActionsForUnitAdvanceInLine(EncounterState state, Entity parent, Unit unit) {
      var rotationComponent = parent.GetComponent<AIRotationComponent>();
      var backSecure = rotationComponent.BackSecure(state, parent, unit);
      rotationComponent.DecideIfShouldRotate(parent, backSecure);
      if (rotationComponent.IsRotating) {
        return ActionsUnitAdvanceRotateOut(state, parent, unit, backSecure);
      } else {
        return ActionsUnitAdvanceFight(state, parent, unit);
      }
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
  }
}