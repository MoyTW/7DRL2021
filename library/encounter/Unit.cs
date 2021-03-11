using System.Collections.Generic;
using System.Text.Json.Serialization;
using Godot;
using SpaceDodgeRL.scenes.components;
using SpaceDodgeRL.scenes.components.AI;
using SpaceDodgeRL.scenes.entities;

namespace SpaceDodgeRL.library.encounter {

  public enum FormationType {
    MANIPULE_CLOSED,
    MANIPULE_OPENED,
    LINE_20
  }

  public enum FormationFacing {
    NORTH,
    EAST,
    SOUTH,
    WEST
  }

  public enum UnitOrder {
    ADVANCE,  // Advance and fight
    RETREAT,  // Retreat behind the next line
    REFORM,   // Reform and rest
    WITHDRAW, // Move off the battlefield
    ROUT    // Flee
  }

  public class AverageTracker {
    [JsonInclude] public float CumulativeAverage { get; private set; } = 0;
    [JsonInclude] public float NumItems { get; private set; } = 0;

    public void AddToAverage(float newValue) {
      if (this.NumItems == 0) {
        this.CumulativeAverage = newValue;
        this.NumItems = 1;
      } else {
        var newAverage = ((this.CumulativeAverage * this.NumItems) + newValue) / (this.NumItems + 1);
        this.CumulativeAverage = newAverage;
        this.NumItems += 1;
      }
    }

    public void SubtractFromAverage(float removeValue) {
      var newAverage = ((this.CumulativeAverage * this.NumItems) - removeValue) / (this.NumItems - 1);
      this.CumulativeAverage = newAverage;
      this.NumItems -= 1;
    }

    public void ReplaceInAverage(float newValue, float oldValue) {
      this.CumulativeAverage = this.CumulativeAverage + ((newValue - oldValue) / this.NumItems);
    }
  }

  public class Unit {
    [JsonInclude] public string UnitId { get; private set; }
    [JsonInclude] public bool LeftFlank { get; private set; }
    [JsonInclude] public bool RightFlank { get; private set; }
    [JsonInclude] public EncounterPosition RallyPoint { get; set; }
    [JsonInclude] public AverageTracker AverageTrackerX { get; private set; }
    [JsonInclude] public AverageTracker AverageTrackerY { get; private set; }
    [JsonIgnore] public EncounterPosition AveragePosition { get {
      return new EncounterPosition(Mathf.RoundToInt(this.AverageTrackerX.CumulativeAverage),
        Mathf.RoundToInt(this.AverageTrackerY.CumulativeAverage));
    } }

    public UnitOrder StandingOrder { get; set; }
    public FormationType UnitFormation { get; set; }
    [JsonIgnore] public int Depth { get {
      return AIUtils.FormationDictionary[this.UnitFormation].Depth(this.NumInFormation);
    } }
    public FormationFacing UnitFacing { get; set; }
    // This is a dumb hack to make it easier to open the manipule, don't refer to it until after unit is fully built,
    // and don't refer to it if there's any chance of this unit dying because then it'll NPE because the unit's not
    // positioned on the map.
    [JsonInclude] public string EntityIdInForPositionZero { get; private set; }
    [JsonInclude] public int OriginalUnitStrength { get; private set; }
    [JsonInclude] public List<string> _BattleReadyEntityIds { get; private set; }
    [JsonInclude] public List<string> _RoutedEntityIds { get; private set; }
    [JsonInclude] public List<string> _DeadEntityIds { get; private set; }
    [JsonIgnore] public int NumInFormation { get { return this._BattleReadyEntityIds.Count; } }

    public Unit(string unitId, EncounterPosition rallyPoint, UnitOrder standingOrder, FormationType unitFormation,
        FormationFacing unitFacing, bool leftFlank, bool rightFlank) {
      this.UnitId = unitId;
      this.LeftFlank = leftFlank;
      this.RightFlank = rightFlank;
      this.RallyPoint = rallyPoint;
      this.AverageTrackerX = new AverageTracker();
      this.AverageTrackerY = new AverageTracker();
      this.StandingOrder = standingOrder;
      this.UnitFormation = unitFormation;
      this.UnitFacing = unitFacing;
      this.EntityIdInForPositionZero = null;
      this.OriginalUnitStrength = 0;
      this._BattleReadyEntityIds = new List<string>();
      this._RoutedEntityIds = new List<string>();
      this._DeadEntityIds = new List<string>();
    }

    // DO NOT use this past the map creation! you shouldn't need to, you should use NotifyEntityRallied() or something
    public void RegisterBattleReadyEntity(Entity entity) {
      this.OriginalUnitStrength += 1;
      this._BattleReadyEntityIds.Add(entity.EntityId);
      var pos = entity.GetComponent<PositionComponent>().EncounterPosition;
      this.AverageTrackerX.AddToAverage(pos.X);
      this.AverageTrackerY.AddToAverage(pos.Y);
      
      if (entity.GetComponent<UnitComponent>().FormationNumber == 0) {
        this.EntityIdInForPositionZero = entity.EntityId;
      }
    }

    public void NotifyEntityMoved(EncounterPosition oldPosition, EncounterPosition newPosition) {
      this.AverageTrackerX.ReplaceInAverage(newPosition.X, oldPosition.X);
      this.AverageTrackerY.ReplaceInAverage(newPosition.Y, oldPosition.Y);
    }

    public void NotifyEntityRouted(Entity entity) {
      this._BattleReadyEntityIds.Remove(entity.EntityId);
      this._RoutedEntityIds.Add(entity.EntityId);
      var pos = entity.GetComponent<PositionComponent>().EncounterPosition;
      this.AverageTrackerX.SubtractFromAverage(pos.X);
      this.AverageTrackerY.SubtractFromAverage(pos.Y);
    }

    public void NotifyEntityRallied(Entity entity) {
      this._RoutedEntityIds.Remove(entity.EntityId);
      this._BattleReadyEntityIds.Add(entity.EntityId);
      var pos = entity.GetComponent<PositionComponent>().EncounterPosition;
      this.AverageTrackerX.AddToAverage(pos.X);
      this.AverageTrackerY.AddToAverage(pos.Y);
    }

    public void NotifyEntityDestroyed(Entity entity) {
      this._BattleReadyEntityIds.Remove(entity.EntityId);
      this._DeadEntityIds.Add(entity.EntityId);
      var pos = entity.GetComponent<PositionComponent>().EncounterPosition;
      this.AverageTrackerX.SubtractFromAverage(pos.X);
      this.AverageTrackerY.SubtractFromAverage(pos.Y);
    }
  }
}