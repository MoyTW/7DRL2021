using System;
using Godot;
using SpaceDodgeRL.library.encounter;

namespace SpaceDodgeRL.scenes.components.AI {
  public static class AIUtils {

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

    public static EncounterPosition PositionInManipuleClosed(int formationNumber, Unit unit) {
      EncounterPosition center = unit.CenterPosition;
      
      int dx = formationNumber % 10;
      int dy = Mathf.FloorToInt(formationNumber / 10) - 1;
      Tuple<int, int> rotated = AIUtils.Rotate(dx, dy, unit.UnitFacing);
      return new EncounterPosition(center.X + rotated.Item1, center.Y + rotated.Item2);
    }

    public static EncounterPosition PositionInManipuleOpened(int formationNumber, Unit unit) {
      int numInFormation = unit.BattleReadyEntities.Count;
      EncounterPosition center = unit.CenterPosition;
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
  }
}