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

    public List<EncounterAction> DecideNextActionForInput(EncounterState state, Entity parent, string actionMapping) {
      if (actionMapping == InputHandler.ActionMapping.MOVE_N) {
        Godot.GD.Print("player AI recognizes player wants to move north!!!");
      }

      var unit = state.GetUnit(parent.GetComponent<UnitComponent>().UnitId);
      var unitComponent = parent.GetComponent<UnitComponent>();

      if (unit.StandingOrder == UnitOrder.REFORM) {
        return AIUtils.ActionsForUnitReform(state, parent, unitComponent.FormationNumber, unit);
      } else if (unit.StandingOrder == UnitOrder.ADVANCE) {
        return AIUtils.ActionsForUnitAdvanceInLine(state, parent, unit);
      } else if (unit.StandingOrder == UnitOrder.RETREAT) {
        return AIUtils.ActionsForUnitRetreat(state, parent, unit);
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