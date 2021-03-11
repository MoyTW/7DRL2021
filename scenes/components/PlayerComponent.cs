using System.Text.Json;
using System.Text.Json.Serialization;
using SpaceDodgeRL.scenes.encounter.state;
using SpaceDodgeRL.scenes.entities;

namespace SpaceDodgeRL.scenes.components {

  public class PlayerComponent : Component {
    public static readonly string ENTITY_GROUP = "PLAYER_COMPONENT_GROUP";
    public string EntityGroup => ENTITY_GROUP;

    private Entity _parent;
    
    [JsonInclude] public bool IsInFormation { get; private set; }
    [JsonInclude] public int Prestige { get; private set; }
    [JsonInclude] public int PilaRange { get; private set; }

    public static PlayerComponent Create(
      bool isInFormation,
      int pilaRange = 3
    ) {
      var component = new PlayerComponent();
      
      component.IsInFormation = isInFormation;
      component.Prestige = 0;
      component.PilaRange = pilaRange;

      return component;
    }

    public void JoinFormation(EncounterState state, Entity parent) {
      this.IsInFormation = true;
      state.GetUnit(parent.GetComponent<UnitComponent>().UnitId).NotifyEntityRallied(parent);
    }

    public void LeaveFormation(EncounterState state, Entity parent) {
      this.IsInFormation = false;
      state.GetUnit(parent.GetComponent<UnitComponent>().UnitId).NotifyEntityRouted(parent);
    }

    public static PlayerComponent Create(string saveData) {
      return JsonSerializer.Deserialize<PlayerComponent>(saveData);
    }

    public void AddPrestige(int dPrestige) {
      this.Prestige += dPrestige;
    }

    public string Save() {
      return JsonSerializer.Serialize(this);
    }

    public void NotifyAttached(Entity parent) {
      this._parent = parent;
    }

    public void NotifyDetached(Entity parent) {
      this._parent = null;
    }
  }
}