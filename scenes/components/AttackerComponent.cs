using System.Text.Json;
using System.Text.Json.Serialization;
using MTW7DRL2021.scenes.entities;

namespace MTW7DRL2021.scenes.components {

  public class AttackerComponent : Component {
    public static readonly string ENTITY_GROUP = "ATTACKER_COMPONENT_GROUP";
    public string EntityGroup => ENTITY_GROUP;

    [JsonInclude] public string SourceEntityId { get; private set; }
    [JsonInclude] public int Power { get; private set; }
    [JsonInclude] public int MeleeAttack { get; set; }
    [JsonInclude] public int RangedAttack { get; private set; }

    public static AttackerComponent Create(string sourceEntityId, int power, int meleeAttack, int rangedAttack) {
      var component = new AttackerComponent();

      component.SourceEntityId = sourceEntityId;
      component.Power = power;
      component.MeleeAttack = meleeAttack;
      component.RangedAttack = rangedAttack;

      return component;
    }

    public static AttackerComponent Create(string saveData) {
      return JsonSerializer.Deserialize<AttackerComponent>(saveData);
    }

    public string Save() {
      return JsonSerializer.Serialize(this);
    }

    public void NotifyAttached(Entity parent) { }

    public void NotifyDetached(Entity parent) { }
  }
}