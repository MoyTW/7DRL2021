using SpaceDodgeRL.library.encounter;
using SpaceDodgeRL.library.encounter.rulebook;
using SpaceDodgeRL.library.encounter.rulebook.actions;
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
      var actions = new List<string>();
      
      if (standingOrder == UnitOrder.ADVANCE && !parent.GetComponent<AIRotationComponent>().IsRotating) {
        // Directly ahead pos
        var validEndPositions = new List<EncounterPosition>() {
          AIUtils.RotateAndProject(parentPos, -1, 0, unit.UnitFacing),
          AIUtils.RotateAndProject(parentPos, -1, -1, unit.UnitFacing),
          AIUtils.RotateAndProject(parentPos, 0, -1, unit.UnitFacing),
          AIUtils.RotateAndProject(parentPos, 1, -1, unit.UnitFacing),
          AIUtils.RotateAndProject(parentPos, 1, 0, unit.UnitFacing),
        };
        foreach (var possible in validEndPositions) {
          var entitiesAtPos = state.EntitiesAtPosition(possible.X, possible.Y);
          if (!AIUtils.IsNextRowTooFarAhead(parentPos, unit) && entitiesAtPos.Count == 0) {
            actions.Add(ToCardinalDirection(parentPos, possible));
          }

          var hostilesAtPos = AIUtils.HostilesInPosition(state, FactionName.PLAYER, possible.X, possible.Y);
          if (hostilesAtPos.Count > 0) {
            actions.Add(ToCardinalDirection(parentPos, possible));
          }
        }
        if (parent.GetComponent<AIRotationComponent>().BackSecure(state, parent, unit)) {
          actions.Add(InputHandler.ActionMapping.ROTATE);
        }
      }

      actions.Add(InputHandler.ActionMapping.WAIT);
      actions.Add(InputHandler.ActionMapping.LEAVE_FORMATION);

      return actions;
    }

    private List<EncounterAction> MoveAndAttack(EncounterState state, int dx, int dy) {
      var positionComponent = state.Player.GetComponent<PositionComponent>();
      var oldPos = positionComponent.EncounterPosition;
      var newPos = new EncounterPosition(oldPos.X + dx, oldPos.Y + dy);

      // TODO: maybe not hostile?
      var hostiles = AIUtils.HostilesInPosition(state, FactionName.PLAYER, newPos.X, newPos.Y);
      if (hostiles.Count > 0) {
        var attackAction = new MeleeAttackAction(state.Player.EntityId, hostiles[state.EncounterRand.Next(hostiles.Count)]);
        return new List<EncounterAction>() { attackAction };
      } else {
        var moveAction = new MoveAction(state.Player.EntityId, newPos);
        return new List<EncounterAction>() { moveAction };
      }
    }

    private List<EncounterAction> HandleMoveCommand(EncounterState state, string actionMapping) {
      if (actionMapping == InputHandler.ActionMapping.MOVE_N) { return MoveAndAttack(state, 0, -1); }
      else if (actionMapping == InputHandler.ActionMapping.MOVE_NE) { return MoveAndAttack(state, 1, -1); }
      else if (actionMapping == InputHandler.ActionMapping.MOVE_E) { return MoveAndAttack(state, 1, 0); }
      else if (actionMapping == InputHandler.ActionMapping.MOVE_SE) { return MoveAndAttack(state, 1, 1); }
      else if (actionMapping == InputHandler.ActionMapping.MOVE_S) { return MoveAndAttack(state, 0, 1); }
      else if (actionMapping == InputHandler.ActionMapping.MOVE_SW) { return MoveAndAttack(state, -1, 1); }
      else if (actionMapping == InputHandler.ActionMapping.MOVE_W) { return MoveAndAttack(state, -1, 0); }
      else if (actionMapping == InputHandler.ActionMapping.MOVE_NW) { return MoveAndAttack(state, -1, -1); }
      else { throw new NotImplementedException(); }
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
        if (this.AllowedActions(state, parent, unit.StandingOrder).Contains(actionMapping) && actionMapping != InputHandler.ActionMapping.WAIT) {
          if (actionMapping == InputHandler.ActionMapping.ROTATE) {
            parent.GetComponent<AIRotationComponent>().PlayerSetRotation(true);
            return null;
          } else {
            return HandleMoveCommand(state, actionMapping);
          }
        } else {
          var actions = AIUtils.ActionsForUnitAdvanceInLine(state, parent, unit);
          // Check the stating autopilot
          if (AIUtils.AdjacentHostiles(state, FactionName.PLAYER, parent.GetComponent<PositionComponent>().EncounterPosition).Count > 0) {
            state.Player.GetComponent<PlayerComponent>().StartOfLevel = false;
            return null;
          }
          foreach (var action in actions) {
            if (action.ActionType == ActionType.MOVE) {
              if (AIUtils.AdjacentHostiles(state, FactionName.PLAYER, ((MoveAction)action).TargetPosition).Count > 0) {
                state.Player.GetComponent<PlayerComponent>().StartOfLevel = false;
                return null;
              }
            }
          }
          return actions;
        }
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