using Godot;
using MTW7DRL2021.library.encounter;
using MTW7DRL2021.library.encounter.rulebook;
using MTW7DRL2021.library.encounter.rulebook.actions;
using MTW7DRL2021.scenes.encounter.state;
using MTW7DRL2021.scenes.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MTW7DRL2021.scenes.components.AI {

  public enum OrderType {
    ADVANCE,
    OPEN_MANIPULE,
    RETREAT,
    ROUT, // This isn't really an "order" but it can be modelled as such
    DECLARE_VICTORY,
    DECLARE_DEFEAT,
    PRINT,
    PREPARE_SWEEP_NEXT_LANE,
    ROTATE_AND_REFORM_AT
  }

  public static class OrderFns {
    public static List<TriggeredOrder> ExecutePREPARE_SWEEP_NEXT_LANE(EncounterState state, Unit unit) {
      // Find your current lane
      Lane closestLane = null;
      double closestDistance = 9999.0;
      foreach (var lane in state.DeploymentInfo.Lanes) {
        var d = lane.LaneCenter.DistanceTo(unit.AveragePosition);
        if (d < closestDistance) {
          closestDistance = d;
          closestLane = lane;
        }
      }
      
      // Find a target lane
      int targetLane = -1;
      for (int i = closestLane.LaneIdx - 1; i >= 0; i--) {
        GD.Print("PREPPING CHECKING: ", i);
        var clear = state.DeploymentInfo.Lanes[i].UnitsForFaction(unit.UnitFaction.Opposite())
          .Select((unitAtLanePosition) => state.GetUnit(unitAtLanePosition.UnitId).StandingOrder)
          .All((order) => order == UnitOrder.ROUT);
        if (!clear) {
          targetLane = i;
          break;
        }
      }
      for (int i = closestLane.LaneIdx + 1; i < state.DeploymentInfo.Lanes.Count; i++) {
        GD.Print("PREPPING CHECKING: ", i);
        var clear = state.DeploymentInfo.Lanes[i].UnitsForFaction(unit.UnitFaction.Opposite())
          .Select((unitAtLanePosition) => state.GetUnit(unitAtLanePosition.UnitId).StandingOrder)
          .All((order) => order == UnitOrder.ROUT);
        if (!clear) {
          targetLane = i;
          break;
        }
      }
      GD.Print("CHOSEN LANE: ", targetLane);
      if (targetLane == -1) {
        return null;
      }

      // Given a target lane, get the closest unit, and advance to its center
      var closestUnroutedEnemy = state.DeploymentInfo.Lanes[targetLane]
        .UnitsForFaction(unit.UnitFaction.Opposite())
        .Last((u) => state.GetUnit((u.UnitId)).StandingOrder != UnitOrder.ROUT);
      var enemyUnit = state.GetUnit(closestUnroutedEnemy.UnitId); 
      var enemyPos = enemyUnit.AveragePosition;
      var vectorToEnemy = AIUtils.VectorFromCenterRotated(unit.AveragePosition, enemyPos.X, enemyPos.Y, unit.UnitFacing);
      var stepsBehind = vectorToEnemy.Item2;
      GD.Print("UNIT IS BEHIND THE ENEMY UNIT BY: ", stepsBehind, " unit pos: ", unit.AveragePosition, " enemy pos: ", enemyPos, "facing: ", unit.UnitFacing);

      // Order unit to march steps + 5
      // unit.StandingOrder = UnitOrder.ADVANCE;

      // Order unit to reform perpendicular to the enemy line
      // var triggerStepsPlusFive = new OrderTrigger(OrderTriggerType.ACTIVATE_ON_OR_AFTER_TURN, false, activateOnTurn: state.CurrentTurn + stepsBehind + 5);
      var oldFacing = unit.UnitFacing;
      var newFacing = vectorToEnemy.Item1 < 0 ? unit.UnitFacing.LeftOf() : unit.UnitFacing.RightOf();
      unit.RallyPoint = AIUtils.RotateAndProject(unit.AveragePosition, 0, -1 * stepsBehind - 25, unit.UnitFacing);;
      unit.UnitFacing = newFacing;
      unit.StandingOrder = UnitOrder.REFORM;

      var behindEnemyUnit = AIUtils.RotateAndProject(enemyUnit.AveragePosition, 0, 15, enemyUnit.UnitFacing);
      
      // Order unit to advance after wheeling
      var triggerStepsPlus40 = new OrderTrigger(OrderTriggerType.ACTIVATE_ON_OR_AFTER_TURN, false, activateOnTurn: state.CurrentTurn + Math.Max(0, stepsBehind) + 15);
      var sweepOrder = new TriggeredOrder(triggerStepsPlus40, new Order(unit.UnitId, OrderType.ROTATE_AND_REFORM_AT, newPosition: behindEnemyUnit, newFacing: oldFacing.Opposite()));

      // Order unit to finally advance
      var triggerStepsPlus60 = new OrderTrigger(OrderTriggerType.ACTIVATE_ON_OR_AFTER_TURN, false, activateOnTurn: state.CurrentTurn + Math.Max(0, stepsBehind) + 50);
      var advance = new TriggeredOrder(triggerStepsPlus60, new Order(unit.UnitId, OrderType.ADVANCE));

      return new List<TriggeredOrder>() { sweepOrder, advance };
    }
  }

  public class Order {
    [JsonInclude] public string UnitId;
    [JsonInclude] public OrderType OrderType;
    [JsonInclude] public FormationFacing NewFacing;
    [JsonInclude] public EncounterPosition? NewPosition;
    
    public Order(string unitId, OrderType orderType, FormationFacing newFacing=FormationFacing.NORTH, EncounterPosition? newPosition=null) {
      this.UnitId = unitId;
      this.OrderType = orderType;
      this.NewFacing = newFacing;
      this.NewPosition = newPosition;
    }

    public List<TriggeredOrder> ExecuteOrder(EncounterState state) {
      var unit = state.GetUnit(this.UnitId);

      if (this.OrderType == OrderType.ADVANCE) {
        unit.StandingOrder = UnitOrder.ADVANCE;
        return null;
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
        return null;
      } else if (this.OrderType == OrderType.ROUT) {
        unit.StandingOrder = UnitOrder.ROUT;
        return null;
      } else if (this.OrderType == OrderType.DECLARE_VICTORY) {
        state.NotifyArmyVictory();
        return null;
      } else if (this.OrderType == OrderType.DECLARE_DEFEAT) {
        state.NotifyArmyDefeat();
        return null;
      } else if (this.OrderType == OrderType.PRINT) {
        GD.Print("!!!!!!! PRINT ORDER EXECUTED !!!!!!!!!");
        return null;
      } else if (this.OrderType == OrderType.PREPARE_SWEEP_NEXT_LANE) {
        return OrderFns.ExecutePREPARE_SWEEP_NEXT_LANE(state, unit);
      } else if (this.OrderType == OrderType.ROTATE_AND_REFORM_AT) {
        if (this.NewPosition == null) {
          unit.RallyPoint = unit.AveragePosition;
        } else {
          unit.RallyPoint = this.NewPosition.Value;
        }
        unit.UnitFacing = this.NewFacing;
        unit.StandingOrder = UnitOrder.REFORM;
        return null;
      } else {
        throw new NotImplementedException("lol: " + this.OrderType);
      }
    }
  }

  public enum OrderTriggerType {
    UNIT_HAS_STANDING_ORDER,
    UNIT_BELOW_STRENGTH_PERCENT,
    ALL_UNITS_OF_FACTION_ROUTED,
    LANE_CLEAR_OF_UNITS_FROM_FACTION,
    ACTIVATE_ON_OR_AFTER_TURN
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
    [JsonInclude] public int ActivateOnTurn { get; private set; }

    public OrderTrigger(OrderTriggerType triggerType, bool repeating, List<string> watchedUnitIds=null,
        List<UnitOrder> awaitedStandingOrders=null, float belowStrengthPercent=9999,
        FactionName triggerFaction=FactionName.NEUTRAL, int activateOnTurn=-1) {
      this.TriggerType = triggerType;
      this.Repeating = repeating;
      this.WatchedUnitIds = watchedUnitIds;
      this.AwaitedStandingOrders = awaitedStandingOrders;
      this.BelowStrengthPercent = belowStrengthPercent;
      this.TriggerFaction = triggerFaction;
      this.ActivateOnTurn = activateOnTurn;
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
      } else if (this.TriggerType == OrderTriggerType.LANE_CLEAR_OF_UNITS_FROM_FACTION) {
        if (this.TriggerFaction == FactionName.NEUTRAL || this.WatchedUnitIds.Count != 1) {
          throw new ArgumentException("didn't set faction or proper watched unit ID, Fs in chat");
        }

        Lane closestLane = null;
        double closestDistance = 9999.0;
        Unit unit = state.GetUnit(this.WatchedUnitIds[0]);
        foreach (var lane in state.DeploymentInfo.Lanes) {
          var d = lane.LaneCenter.DistanceTo(unit.AveragePosition);
          if (d < closestDistance) {
            closestDistance = d;
            closestLane = lane;
          }
        }
        /* if (closestLane == null) {
          GD.PrintErr("ClosestLane null somehow!? wtf.");
          GD.PrintErr(unit.AveragePosition);
          GD.PrintErr(unit.UnitFaction, " lol ", unit.NumInFormation);
          return false;
        } */
        return closestLane.UnitsForFaction(this.TriggerFaction)
          .Select((unitAtLanePosition) => state.GetUnit(unitAtLanePosition.UnitId).StandingOrder)
          .All((order) => order == UnitOrder.ROUT);
      } else if (this.TriggerType == OrderTriggerType.ACTIVATE_ON_OR_AFTER_TURN) {
        if (this.ActivateOnTurn == -1) {
          throw new ArgumentException("didn't set timer");
        }
        if (state.CurrentTurn >= this.ActivateOnTurn) {
          GD.Print(String.Format("Activating trigger on turn {0}, timed for turn {1}!", state.CurrentTurn, this.ActivateOnTurn));
        }
        return state.CurrentTurn >= this.ActivateOnTurn;
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
      GD.Print(String.Format("Registering order - Trigger: {0}, Order: {1}", trigger.TriggerType, order.OrderType));
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
          var unit = state.GetUnit(kvp.Key);
          if (unit.StandingOrder == UnitOrder.ROUT) { continue; }
          var removeThese = new List<TriggeredOrder>();
          var addThese = new List<TriggeredOrder>();

          foreach (var triggeredOrder in kvp.Value) {
            if (triggeredOrder.Trigger.IsTriggered(state)) {
              var newOrders = triggeredOrder.Order.ExecuteOrder(state);
              if (newOrders != null) {
                addThese.AddRange(newOrders);
              }
              if (!triggeredOrder.Trigger.Repeating) {
                removeThese.Add(triggeredOrder);
              }
            }
          }

          foreach (var removeThis in removeThese) {
            kvp.Value.Remove(removeThis);
          }
          foreach (var addThis in addThese) {
            this.RegisterTriggeredOrder(addThis);
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