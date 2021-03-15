using System.Text.Json;
using System.Text.Json.Serialization;
using MTW7DRL2021.scenes.entities;

namespace MTW7DRL2021.scenes.components.use {

  public class UseEffectHealComponent : Component {
    public static readonly string ENTITY_GROUP = "USE_EFFECT_HEAL_COMPONENT_GROUP";
    public string EntityGroup => ENTITY_GROUP;

    [JsonInclude] public int Healpower { get; private set; }

    public static UseEffectHealComponent Create(int healpower) {
      var component = new UseEffectHealComponent();

      component.Healpower = healpower;

      return component;
    }

    public static UseEffectHealComponent Create(string saveData) {
      return JsonSerializer.Deserialize<UseEffectHealComponent>(saveData);
    }

    public string Save() {
      return JsonSerializer.Serialize(this);
    }

    public void NotifyAttached(Entity parent) { }

    public void NotifyDetached(Entity parent) { }
  }
}