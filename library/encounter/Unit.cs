using System.Collections.Generic;
using System.Text.Json.Serialization;

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
    BROKEN    // Flee
  }

  public class Unit {
    [JsonInclude] public string UnitId { get; private set; }
    [JsonInclude] public bool LeftFlank { get; private set; }
    [JsonInclude] public bool RightFlank { get; private set; }
    [JsonInclude] public EncounterPosition RallyPoint { get; set; }
    public UnitOrder StandingOrder { get; set; }
    public FormationType UnitFormation { get; set; }
    public FormationFacing UnitFacing { get; set; }   
    [JsonInclude] public List<string> _BattleReadyEntityIds { get; private set; }
    [JsonIgnore] public int NumInFormation { get { return this._BattleReadyEntityIds.Count; } }

    public Unit(string unitId, EncounterPosition rallyPoint, UnitOrder standingOrder, FormationType unitFormation,
        FormationFacing unitFacing, bool leftFlank, bool rightFlank) {
      this.UnitId = unitId;
      this.LeftFlank = leftFlank;
      this.RightFlank = rightFlank;
      this.RallyPoint = rallyPoint;
      this.StandingOrder = standingOrder;
      this.UnitFormation = unitFormation;
      this.UnitFacing = unitFacing;
      this._BattleReadyEntityIds = new List<string>();
    }
  }
}