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
    public EncounterPosition CenterPosition { get; set; }
    public UnitOrder StandingOrder { get; set; }
    public FormationType UnitFormation { get; set; }
    public FormationFacing UnitFacing { get; set; }   
    [JsonInclude] public List<string> BattleReadyEntities { get; private set; }

    public Unit(string unitId, EncounterPosition centerPosition, UnitOrder standingOrder, FormationType unitFormation, FormationFacing unitFacing) {
      this.UnitId = unitId;
      this.CenterPosition = centerPosition;
      this.StandingOrder = standingOrder;
      this.UnitFormation = unitFormation;
      this.UnitFacing = unitFacing;
      this.BattleReadyEntities = new List<string>();
    }
  }
}