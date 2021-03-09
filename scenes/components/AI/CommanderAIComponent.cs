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
    OPEN_MANIPULE,
    RETREAT
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
        var firstUnit = state.GetEntityById(unit.EntityIdInForPositionZero);

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
      } else if (this.OrderType == OrderType.RETREAT) {
        unit.StandingOrder = UnitOrder.RETREAT;
      } else {
        throw new NotImplementedException();
      }
    }
  }

  public enum OrderTriggerType {
    UNIT_HAS_STANDING_ORDER
  }

  // This is a mess; I'm doing it like this so that the save system can work properly, because I forgot how to set up
  // loading of inherited classes.
  public class OrderTrigger {

    [JsonInclude] public OrderTriggerType TriggerType { get; private set; }
    [JsonInclude] public List<string> WatchedUnitIds { get; private set; }
    [JsonInclude] public List<UnitOrder> AwaitedStandingOrders { get; private set; }

    public OrderTrigger(OrderTriggerType triggerType, List<string> watchedUnitIds=null,
        List<UnitOrder> awaitedStandingOrders=null) {
      this.TriggerType = triggerType;
      this.WatchedUnitIds = watchedUnitIds;
      this.AwaitedStandingOrders = awaitedStandingOrders;
    }

    public bool IsTriggered(EncounterState state) {
      if (this.TriggerType == OrderTriggerType.UNIT_HAS_STANDING_ORDER) {
        foreach (var watchedUnitId in this.WatchedUnitIds) {
          if (this.AwaitedStandingOrders.Contains(state.GetUnit(watchedUnitId).StandingOrder)) {
            return true;
          }
        }
        return false;
      } else {
        throw new NotImplementedException();
      }
    }
  }

  public class TriggeredOrder {
    [JsonInclude] public OrderTrigger Trigger { get; private set; }
    [JsonInclude] public Order Order { get; private set; }
    public TriggeredOrder(OrderTrigger trigger, Order order) {
      this.Trigger = trigger;
      this.Order = order;
    }
  }

  public class CommanderAIComponent : AIComponent {
    public static readonly string ENTITY_GROUP = "COMMANDER_AI_COMPONENT_GROUP";
    public string EntityGroup => ENTITY_GROUP;

    [JsonInclude] public List<string> _CommandedUnitIds { get; private set; }
    [JsonInclude] public int _CurrentTurn { get; private set; }
    [JsonInclude] public Dictionary<int, List<Order>> _DeploymentOrders { get; private set; }
    [JsonInclude] public Dictionary<string, List<TriggeredOrder>> _TriggerOrders { get; private set; }
    [JsonInclude] public int LastDeploymentTurn { get; private set; }
    [JsonInclude] public bool DeploymentComplete { get; private set; }

    public CommanderAIComponent() {
      this._CommandedUnitIds = new List<string>();
      this._CurrentTurn = 0;
      this._DeploymentOrders = new Dictionary<int, List<Order>>();
      this._TriggerOrders = new Dictionary<string, List<TriggeredOrder>>();
      this.LastDeploymentTurn = 0;
      this.DeploymentComplete = true;
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

      if (turn > this.LastDeploymentTurn) {
        this.LastDeploymentTurn = turn;
      }
      this.DeploymentComplete = false;
    }

    public void RegisterTriggeredOrder(OrderTrigger trigger, Order order) {
      if (!this._TriggerOrders.ContainsKey(order.UnitId)) {
        this._TriggerOrders[order.UnitId] = new List<TriggeredOrder>();
      }
      this._TriggerOrders[order.UnitId].Add(new TriggeredOrder(trigger, order));
    }

    public List<EncounterAction> DecideNextAction(EncounterState state, Entity parent) {
      if (this._DeploymentOrders.ContainsKey(this._CurrentTurn)) {
        var deploymentOrders = this._DeploymentOrders[this._CurrentTurn];
        foreach (var order in deploymentOrders) {
          order.ExecuteOrder(state);
        }
        if (this.LastDeploymentTurn == this._CurrentTurn) {
          this.DeploymentComplete = true;
        }
      } else if (this.DeploymentComplete) {
        foreach (var kvp in this._TriggerOrders) {
          foreach (var triggeredOrder in kvp.Value) {
            if (triggeredOrder.Trigger.IsTriggered(state)) {
              triggeredOrder.Order.ExecuteOrder(state);
            }
          }
        }

        foreach (var unitId in this._CommandedUnitIds) {
          var unit = state.GetUnit(unitId);
          if (unit.NumInFormation < unit.OriginalUnitStrength - 15) {
            new Order(unitId, OrderType.RETREAT).ExecuteOrder(state);
          }
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