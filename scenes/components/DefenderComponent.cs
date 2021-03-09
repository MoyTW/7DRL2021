using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using SpaceDodgeRL.scenes.entities;

namespace SpaceDodgeRL.scenes.components {

  public class DefenderComponent : Component {
    public static readonly string ENTITY_GROUP = "DEFENDER_COMPONENT_GROUP";
    public string EntityGroup => ENTITY_GROUP;

    [JsonInclude] public int BaseDR { get; private set; }
    // Right now we don't do defense buffs, but we could later!
    public int Defense { get => this.BaseDR; }

    [JsonInclude] public int MaxHp { get; private set; }
    [JsonInclude] public int CurrentHp { get; private set; }

    /**
     * Footing is used as a stat malus / temporary HP.
     * 100-70%: no penalty
     * 69-50%: -10 flat to all stats
     * 49-30%: -20 flat to all stats
     * 29-10%: -30 flat to all stats
     * 00-09%: you are prone and all attacks you make miss and all attacks against you hit
     *
     * It costs 5 footing to attack.
     *
     * Waiting restores all footing.
     *
     * If you take a hit, footing will shield for floor(damage * footing%), and take 3*floor(damage * footing%). So if
     * you get hit at 100% footing it ALL goes to footing, and then you start taking damage from there.
     */
    [JsonInclude] public int MaxFooting { get; private set; }
    [JsonInclude] public int CurrentFooting { get; private set; }
    [JsonIgnore] public double PercentageFooting { get => (double)this.CurrentFooting / (double)this.MaxFooting; }
    [JsonIgnore] public int FootingPenalty { get {
      if (this.PercentageFooting >= .7) {
        return 0;
      } else if (this.PercentageFooting >= .5) {
        return 10;
      } else if (this.PercentageFooting >= .3) {
        return 20;
      } else if (this.PercentageFooting >= .2) {
        return 30;
      } else {
        return 9999;
      }
    } }

    [JsonInclude] public int MeleeDefense { get; private set; }
    [JsonInclude] public int RangedDefense { get; private set; }

    [JsonInclude] public bool ShouldLogDamage { get; private set; }
    [JsonInclude] public bool IsInvincible { get; private set; }

    public static DefenderComponent Create(int baseDefense, int maxHp, int maxFooting, int meleeDefense,
        int rangedDefense, int currentHp = int.MinValue, bool logDamage = true, bool isInvincible = false) {
      var component = new DefenderComponent();

      component.BaseDR = baseDefense;
      component.MaxHp = maxHp;
      if (currentHp == int.MinValue) {
        component.CurrentHp = component.MaxHp;
      } else {
        component.CurrentHp = currentHp;
      }
      component.MaxFooting = maxFooting;
      component.CurrentFooting = maxFooting;
      component.MeleeDefense = meleeDefense;
      component.RangedDefense = rangedDefense;
      component.ShouldLogDamage = logDamage;
      component.IsInvincible = isInvincible;

      return component;
    }

    public static DefenderComponent Create(string saveData) {
      return JsonSerializer.Deserialize<DefenderComponent>(saveData);
    }

    public void AddBaseMaxHp(int hp, bool alsoAddCurrentHp = true) {
      this.MaxHp += hp;
      if (alsoAddCurrentHp) {
        this.CurrentHp += hp;
      }
    }

    /**
     * Returns the number of HP restored.
     */
    public int RestoreHP(int hp, bool overheal = false) {
      int startingHp = this.CurrentHp;

      this.CurrentHp += hp;
      if (!overheal && this.CurrentHp > this.MaxHp) {
        this.CurrentHp = this.MaxHp;
      }

      return this.CurrentHp - startingHp;
    }

    /**
     * Directly removes HP, without any checks
     */
    public void RemoveHp(int hp) {
      this.CurrentHp -= hp;
    }

    public void NotifyParentHasAttacked() {
      this.CurrentFooting -= 5;
    }

    public void RemoveFooting(int footing) {
      this.CurrentFooting -= footing;
    }

    public void RestoreFooting() {
      this.CurrentFooting = this.MaxFooting;
    }

    public string Save() {
      return JsonSerializer.Serialize(this);
    }

    public void NotifyAttached(Entity parent) { }

    public void NotifyDetached(Entity parent) { }
  }
}