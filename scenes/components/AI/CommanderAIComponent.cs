using Godot;
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

  public enum OrderType {
    ADVANCE,
    OPEN_MANIPULE
  }

  public class Order {
    [JsonInclude] public string UnitId;
    [JsonInclude] public OrderType OrderType;
    
    public Order(string unitId, OrderType orderType) {
      this.UnitId = unitId;
      this.OrderType = orderType;
    }

    public void ExecuteOrder(EncounterState state) {
      var unit = state.GetUnit(this.UnitId);

      if (this.OrderType == OrderType.ADVANCE) {
        unit.StandingOrder = UnitOrder.ADVANCE;
      } else if (this.OrderType == OrderType.OPEN_MANIPULE) {
        var firstUnitId = unit._BattleReadyEntityIds.First( // TODO: not just hastatus
          (u) => state.GetEntityById(u).GetComponent<HastatusAIComponent>().FormationNumber == 0);
        var firstUnit = state.GetEntityById(firstUnitId);

        unit.UnitFormation = FormationType.MANIPULE_OPENED;
        unit.StandingOrder = UnitOrder.REFORM;
        // TODO: dumb hack to make blocks line up
        var firstUnitPos = firstUnit.GetComponent<PositionComponent>().EncounterPosition;
        if (unit.UnitFacing == FormationFacing.SOUTH) {
          unit.RallyPoint = new EncounterPosition(firstUnitPos.X + 1, firstUnitPos.Y);
        } else if (unit.UnitFacing == FormationFacing.WEST) {
          unit.RallyPoint = new EncounterPosition(firstUnitPos.X, firstUnitPos.Y + 1);
        } else {
          unit.RallyPoint = firstUnitPos;
        }
      } else {
        throw new NotImplementedException();
      }
    }
  }

  public class CommanderAIComponent : AIComponent {
    public static readonly string ENTITY_GROUP = "COMMANDER_AI_COMPONENT_GROUP";
    public string EntityGroup => ENTITY_GROUP;

    [JsonInclude] public List<string> _CommandedUnitIds { get; private set; }
    [JsonInclude] public int _CurrentTurn { get; private set; }
    [JsonInclude] public Dictionary<int, List<Order>> _DeploymentOrders { get; private set; }

    public CommanderAIComponent() {
      this._CommandedUnitIds = new List<string>();
      this._CurrentTurn = 0;
      this._DeploymentOrders = new Dictionary<int, List<Order>>();
    }

    public static CommanderAIComponent Create(string saveData) {
      return JsonSerializer.Deserialize<CommanderAIComponent>(saveData);
    }

    public void RegisterUnit(Unit unit) {
      this._CommandedUnitIds.Add(unit.UnitId);
    }

    public void RegisterDeploymentOrder(int turn, Order order) {
      if (!this._DeploymentOrders.ContainsKey(turn)) {
        this._DeploymentOrders[turn] = new List<Order>();
      }
      this._DeploymentOrders[turn].Add(order);
    }

    public List<EncounterAction> DecideNextAction(EncounterState state, Entity parent) {
      if (this._DeploymentOrders.ContainsKey(this._CurrentTurn)) {
        var deploymentOrders = this._DeploymentOrders[this._CurrentTurn];
        foreach (var order in deploymentOrders) {
          order.ExecuteOrder(state);
        }
      }

      this._CurrentTurn += 1;
      return new List<EncounterAction>() { new WaitAction(parent.EntityId) };
    }

    public string Save() {
      return JsonSerializer.Serialize(this);
    }

    public void NotifyAttached(Entity parent) { }

    public void NotifyDetached(Entity parent) { }
  }
}