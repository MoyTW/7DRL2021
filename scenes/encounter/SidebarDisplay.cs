using System.Collections.Generic;
using System.Text;
using Godot;
using SpaceDodgeRL.scenes.components;
using SpaceDodgeRL.scenes.encounter.state;
using SpaceDodgeRL.scenes.entities;

namespace SpaceDodgeRL.scenes.encounter {

  public class SidebarDisplay : TextureRect {

    private RichTextLabel _encounterLogLabel;

    public override void _Ready() {
      this._encounterLogLabel = this.GetNode<RichTextLabel>("SidebarVBox/EncounterLogLabel");
      GetNode<VBoxContainer>("SidebarVBox/ScanOptionBlock/ScanBlock").Hide();
      GetNode<Label>("SidebarVBox/ScanOptionBlock/NoEntityScannedLabel").Show();
    }

    public void AddEncounterLogMessage(string bbCodeMessage, int encounterLogSize) {
      if (this._encounterLogLabel.GetLineCount() > encounterLogSize) {
        this._encounterLogLabel.RemoveLine(0);
      }
      this._encounterLogLabel.AppendBbcode(bbCodeMessage + "\n");
      this._encounterLogLabel.Update();
    }

    public void RefreshStats(EncounterState state) {
      var player = state.Player;

      // Left column
      var playerDefenderComponent = player.GetComponent<DefenderComponent>();
      var newHPText = string.Format("HP: {0}/{1}", playerDefenderComponent.CurrentHp, playerDefenderComponent.MaxHp);
      GetNode<Label>("SidebarVBox/StatsAndPositionHBox/StatsBlock/HPLabel").Text = newHPText;

      var newFootingText = string.Format("Footing: {0}/{1}", playerDefenderComponent.CurrentFooting,
        playerDefenderComponent.MaxFooting, playerDefenderComponent.FootingPenalty);
      GetNode<Label>("SidebarVBox/StatsAndPositionHBox/StatsBlock/FootingLabel").Text = newFootingText;

      var playerAttackerComponent = player.GetComponent<AttackerComponent>();
      var newMAtkText = string.Format("Melee Attack: {0}", playerAttackerComponent.MeleeAttack);
      GetNode<Label>("SidebarVBox/StatsAndPositionHBox/StatsBlock/MeleeAttackLabel").Text = newMAtkText;

      var newAttackPowerText = string.Format("Attack Power: {0}", playerAttackerComponent.Power);
      GetNode<Label>("SidebarVBox/StatsAndPositionHBox/StatsBlock/AttackPowerLabel").Text = newAttackPowerText;

      var newMDefText = string.Format("Melee Defense: {0}", playerDefenderComponent.MeleeDefense);
      GetNode<Label>("SidebarVBox/StatsAndPositionHBox/StatsBlock/MeleeDefenseLabel").Text = newMDefText;

      var newRDefText = string.Format("Ranged Defense: {0}", playerDefenderComponent.RangedDefense);
      GetNode<Label>("SidebarVBox/StatsAndPositionHBox/StatsBlock/RangedDefenseLabel").Text = newRDefText;

      var speedComponent = player.GetComponent<SpeedComponent>();
      var newSpeedText = string.Format("Speed: {0}", speedComponent.Speed);
      GetNode<Label>("SidebarVBox/StatsAndPositionHBox/StatsBlock/SpeedLabel").Text = newSpeedText;

      // Right column
      var playerPos = player.GetComponent<PositionComponent>().EncounterPosition;

      var newTurnReadoutText = string.Format("Turn: {0:0.00}", state.CurrentTick / 100);
      GetNode<Label>("SidebarVBox/StatsAndPositionHBox/PositionBlock/TurnReadoutLabel").Text = newTurnReadoutText;

      var playerComponent = player.GetComponent<PlayerComponent>();
      var newPretigeText = string.Format("Prestige: {0}", playerComponent.Prestige);
      GetNode<Label>("SidebarVBox/StatsAndPositionHBox/PositionBlock/PrestigeLabel").Text = newPretigeText;

      var xpComponent = player.GetComponent<XPTrackerComponent>();
      var newLevelText = string.Format("Level: {0}", xpComponent.Level);
      GetNode<Label>("SidebarVBox/StatsAndPositionHBox/PositionBlock/LevelLabel").Text = newLevelText;
      var newXPText = string.Format("Experience: {0}/{1}", xpComponent.XP, xpComponent.NextLevelAtXP);
      GetNode<Label>("SidebarVBox/StatsAndPositionHBox/PositionBlock/ExperienceLabel").Text = newXPText;

      var unit = state.GetUnit(player.GetComponent<UnitComponent>().UnitId);
      var unitOrderText = string.Format("Order: {0}", unit.StandingOrder.ToString());
      GetNode<Label>("SidebarVBox/StatsAndPositionHBox/PositionBlock/UnitOrderLabel").Text = unitOrderText;

      var unitSizeText = string.Format("Strength: {0}/{1}", unit.NumInFormation, unit.OriginalUnitStrength);
      GetNode<Label>("SidebarVBox/StatsAndPositionHBox/PositionBlock/UnitNumLabel").Text = unitSizeText;
    }

    public void DisplayScannedEntity(int x, int y, Entity entity) {
      if (entity == null) {
        GetNode<VBoxContainer>("SidebarVBox/ScanOptionBlock/ScanBlock").Hide();
        GetNode<Label>("SidebarVBox/ScanOptionBlock/NoEntityScannedLabel").Show();
      } else {
        GetNode<Label>("SidebarVBox/ScanOptionBlock/NoEntityScannedLabel").Hide();
        GetNode<VBoxContainer>("SidebarVBox/ScanOptionBlock/ScanBlock").Show();

        var scanTextureRect = GetNode<TextureRect>("SidebarVBox/ScanOptionBlock/ScanBlock/ReadoutTextureName/ReadoutTextureRect");
        scanTextureRect.Texture = entity.GetComponent<PositionComponent>().SpriteTexture;

        var scanNameLabel = GetNode<Label>("SidebarVBox/ScanOptionBlock/ScanBlock/ReadoutTextureName/ScanReadoutName");
        scanNameLabel.Text = entity.EntityName;

        var descriptionLabel = GetNode<RichTextLabel>("SidebarVBox/ScanOptionBlock/ScanBlock/DescriptionLabel");

        var descBuilder = new StringBuilder();

        var attackerComponent = entity.GetComponent<AttackerComponent>();
        if (attackerComponent != null) {
          descBuilder.AppendLine(string.Format("[b]Damage:[/b] {0}", attackerComponent.Power));
        }

        var defenderComponent = entity.GetComponent<DefenderComponent>();
        if (defenderComponent != null) {
          if (defenderComponent.IsInvincible) {
            descBuilder.AppendLine("[b]Invincible[/b]");
          } else {
            descBuilder.AppendLine(string.Format("[b]HP:[/b] {0}/{1}", defenderComponent.CurrentHp, defenderComponent.MaxHp));
            descBuilder.AppendLine(string.Format("[b]Armor:[/b] {0}", defenderComponent.BaseDR));
          }
        }

        if (entity.GetComponent<SpeedComponent>() != null) {
          descBuilder.AppendLine(string.Format("[b]Speed:[/b] {0}", entity.GetComponent<SpeedComponent>().Speed));
        }

        var xpValueComponent = entity.GetComponent<XPValueComponent>();
        if (xpValueComponent != null) {
          descBuilder.AppendLine(string.Format("[b]XP Value:[/b] {0}", xpValueComponent.XPValue));
        }

        descBuilder.AppendLine(entity.GetComponent<DisplayComponent>().Description);

        descriptionLabel.BbcodeText = descBuilder.ToString();
      }
    }
  }
}