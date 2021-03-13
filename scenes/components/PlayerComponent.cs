using System.Text.Json;
using System.Text.Json.Serialization;
using SpaceDodgeRL.scenes.encounter.state;
using SpaceDodgeRL.scenes.entities;

namespace SpaceDodgeRL.scenes.components {

  public class PlayerComponent : Component {
    public static readonly string ENTITY_GROUP = "PLAYER_COMPONENT_GROUP";
    public string EntityGroup => ENTITY_GROUP;

    private Entity _parent;
    
    // If start of level you should autopilot
    [JsonInclude] public bool SeenIntroFormUp { get; set; }
    [JsonInclude] public bool SeenIntroBattle { get; set; }
    [JsonInclude] public bool StartOfLevel { get; set; }
    [JsonInclude] public bool IsInFormation { get; private set; }
    [JsonInclude] public int Prestige { get; private set; }
    [JsonInclude] public int PilaRange { get; private set; }

    public static PlayerComponent Create(
      bool isInFormation,
      int pilaRange = 3
    ) {
      var component = new PlayerComponent();
      
      component.SeenIntroFormUp = false;
      component.SeenIntroBattle = false;
      component.StartOfLevel = true;
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
      this.AddPrestige(-10, state, "Your allies will remember how you broke from formation! [b]You lose 10 prestige.[/b]");
    }

    public static PlayerComponent Create(string saveData) {
      return JsonSerializer.Deserialize<PlayerComponent>(saveData);
    }

    public void AddPrestige(int dPrestige, EncounterState state, string message) {
      this.Prestige += dPrestige;
      state.LogMessage(message);
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