using SpaceDodgeRL.library.encounter;
using SpaceDodgeRL.library.encounter.rulebook;
using SpaceDodgeRL.scenes.encounter.state;
using SpaceDodgeRL.scenes.entities;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace SpaceDodgeRL.scenes.components.AI {

  /**
   * Not actually an AI component! used to autopilot the player! possibly literally just merge this into the Player
   * component or something!
   */
  public class PlayerAIComponent : Component {
    public static readonly string ENTITY_GROUP = "PLAYER_AI_COMPONENT_GROUP";
    public string EntityGroup => ENTITY_GROUP;

    public PlayerAIComponent() { }

    public static PlayerAIComponent Create(string saveData) {
      return JsonSerializer.Deserialize<PlayerAIComponent>(saveData);
    }

    public string ToCardinalDirection(EncounterPosition parentPos, EncounterPosition adjacentPos) {
      int dx = adjacentPos.X - parentPos.X;
      int dy = adjacentPos.Y - parentPos.Y;
      if (dx == 0 && dy == -1) {
        return InputHandler.ActionMapping.MOVE_N;
      } else if (dx == 1 && dy == -1) {
        return InputHandler.ActionMapping.MOVE_NE;
      } else if (dx == 1 && dy == 0) {
        return InputHandler.ActionMapping.MOVE_E;
      } else if (dx == 1 && dy == 1) {
        return InputHandler.ActionMapping.MOVE_SE;
      } else if (dx == 0 && dy == 1) {
        return InputHandler.ActionMapping.MOVE_S;
      } else if (dx == -1 && dy == 1) {
        return InputHandler.ActionMapping.MOVE_SW;
      } else if (dx == -1 && dy == 0) {
        return InputHandler.ActionMapping.MOVE_W;
      } else if (dx == -1 && dy == -1) {
        return InputHandler.ActionMapping.MOVE_NW;
      } else {
        throw new NotImplementedException();
      }
    }

    public List<string> AllowedActions(EncounterState state, Entity parent, UnitOrder standingOrder) {
      var parentPos = parent.GetComponent<PositionComponent>().EncounterPosition;
      var unit = state.GetUnit(parent.GetComponent<UnitComponent>().UnitId);
      var actions = new List<string>() { InputHandler.ActionMapping.LEAVE_FORMATION, InputHandler.ActionMapping.WAIT };
      
      if (standingOrder == UnitOrder.ADVANCE) {
        // Directly ahead pos
        var validEndPositions = new List<EncounterPosition>() {
          AIUtils.RotateAndProject(parentPos, -1, 0, unit.UnitFacing),
          AIUtils.RotateAndProject(parentPos, -1, -1, unit.UnitFacing),
          AIUtils.RotateAndProject(parentPos, 0, -1, unit.UnitFacing),
          AIUtils.RotateAndProject(parentPos, 1, -1, unit.UnitFacing),
          AIUtils.RotateAndProject(parentPos, 1, 0, unit.UnitFacing),
        };
        foreach (var possible in validEndPositions) {
          if (state.EntitiesAtPosition(possible.X, possible.Y).Count == 0) {
            actions.Add(ToCardinalDirection(parentPos, possible));
          }
        }
      }

      return actions;
    }

    public List<EncounterAction> DecideNextActionForInput(EncounterState state, Entity parent, string actionMapping) {
      var unit = state.GetUnit(parent.GetComponent<UnitComponent>().UnitId);
      var unitComponent = parent.GetComponent<UnitComponent>();

      if (actionMapping == InputHandler.ActionMapping.LEAVE_FORMATION) {
        parent.GetComponent<PlayerComponent>().LeaveFormation(state, parent);
        return null;
      }

      if (unit.StandingOrder == UnitOrder.REFORM) {
        if (actionMapping == InputHandler.ActionMapping.WAIT) {
          return AIUtils.ActionsForUnitReform(state, parent, unitComponent.FormationNumber, unit);
        } else {
          return null;
        }
      } else if (unit.StandingOrder == UnitOrder.ADVANCE) {
        return AIUtils.ActionsForUnitAdvanceInLine(state, parent, unit);
      } else if (unit.StandingOrder == UnitOrder.RETREAT) {
        if (actionMapping == InputHandler.ActionMapping.WAIT) {
          return AIUtils.ActionsForUnitRetreat(state, parent, unit);
        } else {
          return null;
        }
      } else if (unit.StandingOrder == UnitOrder.ROUT) {
        return null;
      } else {
        throw new NotImplementedException();
      }
    }

    public string Save() {
      return JsonSerializer.Serialize(this);
    }

    public void NotifyAttached(Entity parent) { }

    public void NotifyDetached(Entity parent) { }
  }
}