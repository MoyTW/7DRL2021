using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using MTW7DRL2021.scenes.encounter.state;
using MTW7DRL2021.scenes.entities;

namespace MTW7DRL2021.scenes.components {

  public enum PrestigeSource {
    VICTORIES,
    DEFEATING_FOES,
    LANDING_HITS,
    ROTATING,
    BREAKING_FORMATION,
    FLEEING
  }

  public class PlayerComponent : Component {
    public static readonly string ENTITY_GROUP = "PLAYER_COMPONENT_GROUP";
    public string EntityGroup => ENTITY_GROUP;

    private Entity _parent;
    
    // If start of level you should autopilot
    [JsonInclude] public bool SeenIntroFormUp { get; set; }
    [JsonInclude] public bool SeenIntroBattle { get; set; }
    [JsonInclude] public bool StartOfLevel { get; set; }
    [JsonInclude] public bool IsInFormation { get; set; }
    [JsonIgnore] public int Prestige { get {
      return this._PrestigeBySource.Values.Sum();
    } }
    [JsonInclude] public Dictionary<PrestigeSource, int> _PrestigeBySource { get; private set; }
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
      component._PrestigeBySource = new Dictionary<PrestigeSource, int>() {
        { PrestigeSource.VICTORIES, 0 },
        { PrestigeSource.DEFEATING_FOES, 0 },
        { PrestigeSource.LANDING_HITS, 0 },
        { PrestigeSource.ROTATING, 0 },
        { PrestigeSource.BREAKING_FORMATION, 0 },
        { PrestigeSource.FLEEING, 0 },
      };
      component.PilaRange = pilaRange;

      return component;
    }

    public void LeaveFormation(EncounterState state, Entity parent) {
      this.IsInFormation = false;
      state.GetUnit(parent.GetComponent<UnitComponent>().UnitId).NotifyEntityRouted(parent);
      this.AddPrestige(-10, state, "Your allies will remember how you broke from formation! [b]You lose 10 prestige.[/b]", PrestigeSource.BREAKING_FORMATION);
    }

    public static PlayerComponent Create(string saveData) {
      return JsonSerializer.Deserialize<PlayerComponent>(saveData);
    }

    public void AddPrestige(int dPrestige, EncounterState state, string message, PrestigeSource source) {
      this._PrestigeBySource[source] += dPrestige;
      state.LogMessage(message);
    }

    public int PrestigeFrom(PrestigeSource source) {
      return this._PrestigeBySource[source];
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