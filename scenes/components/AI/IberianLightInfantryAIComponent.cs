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

  public class IberianLightInfantryAIComponent : DeciderAIComponent {
    public static readonly string ENTITY_GROUP = "IBERIAN_LIGHT_INFANTRY_AI_COMPONENT_GROUP";
    public override string EntityGroup => ENTITY_GROUP;

    [JsonInclude] public int FormationNumber { get; private set; }
    [JsonInclude] public string UnitId { get; private set; }

    public int TestTimer { get; set; }

    public IberianLightInfantryAIComponent(int formationNumber, string unitId) {
      this.FormationNumber = formationNumber;
      this.UnitId = unitId;

      this.TestTimer = 0;
    }

    public static IberianLightInfantryAIComponent Create(string saveData) {
      return JsonSerializer.Deserialize<IberianLightInfantryAIComponent>(saveData);
    }

    private List<EncounterAction> _ActionsForUnitAdvance(EncounterState state, Entity parent) {
      var actions = new List<EncounterAction>();

      var parentPos = parent.GetComponent<PositionComponent>().EncounterPosition;
      var parentFaction = parent.GetComponent<FactionComponent>().Faction;

      var moveVec = AIUtils.Rotate(0, -1, state.GetUnit(this.UnitId).UnitFacing);
      var targetPos = new EncounterPosition(parentPos.X + moveVec.Item1, parentPos.Y + moveVec.Item2);
      if (state.EntitiesAtPosition(targetPos.X, targetPos.Y).Count == 0) {
        // Move
        actions.Add(new MoveAction(parent.EntityId, targetPos));
        // Attack TODO: replace this with charge
        var adjacentHostiles = AIUtils.AdjacentHostiles(state, parentFaction, targetPos);
        // TODO: don't attack randomly
        if (adjacentHostiles.Count > 0) {
          var target = adjacentHostiles[state.EncounterRand.Next(adjacentHostiles.Count)];
          actions.Add(new MeleeAttackAction(parent.EntityId, target));
        }
      } else {
        var adjacentHostiles = AIUtils.AdjacentHostiles(state, parentFaction, parentPos);
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
        return AIUtils.ActionsForUnitReform(state, parent, this.FormationNumber, this.UnitId);
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