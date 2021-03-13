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
    RETREAT,
    ROUT, // This isn't really an "order" but it can be modelled as such
    DECLARE_VICTORY,
    DECLARE_DEFEAT
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
      } else if (this.OrderType == OrderType.ROUT) {
        unit.StandingOrder = UnitOrder.ROUT;
      } else if (this.OrderType == OrderType.DECLARE_VICTORY) {
        state.NotifyArmyVictory();
      } else if (this.OrderType == OrderType.DECLARE_DEFEAT) {
        state.NotifyArmyDefeat();
      } else {
        throw new NotImplementedException();
      }
    }
  }

  public enum OrderTriggerType {
    UNIT_HAS_STANDING_ORDER,
    UNIT_BELOW_STRENGTH_PERCENT,
    ALL_UNITS_OF_FACTION_ROUTED,
    LANE_CLEAR_OF_UNITS_FROM_FACTION
  }

  // This is a mess; I'm doing it like this so that the save system can work properly, because I forgot how to set up
  // loading of inherited classes.
  public class OrderTrigger {

    [JsonInclude] public OrderTriggerType TriggerType { get; private set; }
    [JsonInclude] public bool Repeating { get; private set; }
    [JsonInclude] public List<string> WatchedUnitIds { get; private set; }
    [JsonInclude] public List<UnitOrder> AwaitedStandingOrders { get; private set; }
    [JsonInclude] public float BelowStrengthPercent { get; private set; }
    [JsonInclude] public FactionName TriggerFaction { get; private set; }

    public OrderTrigger(OrderTriggerType triggerType, bool repeating, List<string> watchedUnitIds=null,
        List<UnitOrder> awaitedStandingOrders=null, float belowStrengthPercent=9999,
        FactionName triggerFaction=FactionName.NEUTRAL) {
      this.TriggerType = triggerType;
      this.Repeating = repeating;
      this.WatchedUnitIds = watchedUnitIds;
      this.AwaitedStandingOrders = awaitedStandingOrders;
      this.BelowStrengthPercent = belowStrengthPercent;
      this.TriggerFaction = triggerFaction;
    }

    public bool IsTriggered(EncounterState state) {
      if (this.TriggerType == OrderTriggerType.UNIT_HAS_STANDING_ORDER) {
        foreach (var watchedUnitId in this.WatchedUnitIds) {
          if (this.AwaitedStandingOrders.Contains(state.GetUnit(watchedUnitId).StandingOrder)) {
            return true;
          }
        }
        return false;
      } else if (this.TriggerType == OrderTriggerType.UNIT_BELOW_STRENGTH_PERCENT) {
        foreach (var watchedUnitId in this.WatchedUnitIds) {
          var unit = state.GetUnit(watchedUnitId);
          if ((float)unit.NumInFormation / (float)unit.OriginalUnitStrength < this.BelowStrengthPercent) {
            return true;
          }
        }
        return false;
      } else if (this.TriggerType == OrderTriggerType.ALL_UNITS_OF_FACTION_ROUTED) {
        foreach (var unit in state.GetUnitsOfFaction(this.TriggerFaction)) {
          if (unit.StandingOrder != UnitOrder.ROUT) {
            return false;
          }
        }
        return true;
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

    public static TriggeredOrder AdvanceIfUnitRetreatsRoutsOrWithdraws(Unit watchedUnit, Unit advanceUnit) {
      var trigger = new OrderTrigger(OrderTriggerType.UNIT_HAS_STANDING_ORDER, false,
        new List<string>() { watchedUnit.UnitId }, new List<UnitOrder>() { UnitOrder.ROUT });
      return new TriggeredOrder(trigger, new Order(advanceUnit.UnitId, OrderType.ADVANCE));
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

    public void RegisterTriggeredOrder(TriggeredOrder triggeredOrder) {
      if (!this._TriggerOrders.ContainsKey(triggeredOrder.Order.UnitId)) {
        this._TriggerOrders[triggeredOrder.Order.UnitId] = new List<TriggeredOrder>();
      }
      this._TriggerOrders[triggeredOrder.Order.UnitId].Add(triggeredOrder);
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
          var removeThese = new List<TriggeredOrder>();

          foreach (var triggeredOrder in kvp.Value) {
            if (triggeredOrder.Trigger.IsTriggered(state)) {
              triggeredOrder.Order.ExecuteOrder(state);
              if (!triggeredOrder.Trigger.Repeating) {
                removeThese.Add(triggeredOrder);
              }
            }
          }

          foreach (var removeThis in removeThese) {
            kvp.Value.Remove(removeThis);
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