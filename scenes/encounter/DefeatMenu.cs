using System;
using Godot;
using MTW7DRL2021.scenes.components;
using MTW7DRL2021.scenes.encounter.state;
using MTW7DRL2021.scenes.singletons;

public class DefeatMenu : VBoxContainer {

  private Button _mainMenuBotton;
  private EncounterState _state;

  public override void _Ready() {
    this._mainMenuBotton = this.GetNode<Button>("MainMenuButton");
    this._mainMenuBotton.Connect("pressed", this, nameof(OnMainMenuBttonPressed));
    this.GetNode<Button>("SaveAndQuitButton").Connect("pressed", this, nameof(OnSaveAndQuitButtonPressed));
  }

  public void PrepMenu(EncounterState state) {
    this._mainMenuBotton.GrabFocus();
    this._state = state;

    var playerComponent = state.Player.GetComponent<PlayerComponent>();

    Label prestigeTotalLabel = GetNode<Label>("PrestigeTotalLabel");
    prestigeTotalLabel.Text = String.Format("You have fallen with {0} prestige.", playerComponent.Prestige);

    Label prestigeVictoriesLabel = GetNode<Label>("PrestigeVictoriesLabel");
    prestigeVictoriesLabel.Text = String.Format("You gained {0} prestige from your victories in battle.", playerComponent.PrestigeFrom(PrestigeSource.VICTORIES));

    Label prestigeDefeatingFoesLabel = GetNode<Label>("PrestigeDefeatingFoesLabel");
    prestigeDefeatingFoesLabel.Text = String.Format("You gained {0} prestige from defeating foes.", playerComponent.PrestigeFrom(PrestigeSource.DEFEATING_FOES));

    Label prestigeLandingHitsLabel = GetNode<Label>("PrestigeLandingHitsLabel");
    prestigeLandingHitsLabel.Text = String.Format("You gained {0} prestige from landing hits on foes.", playerComponent.PrestigeFrom(PrestigeSource.LANDING_HITS));

    Label prestigeRotatingLabel = GetNode<Label>("PrestigeRotatingLabel");
    prestigeRotatingLabel.Text = String.Format("You lost {0} prestige from rotating to the rear.", playerComponent.PrestigeFrom(PrestigeSource.ROTATING));  
    
    Label prestigeBreakingFormationLabel = GetNode<Label>("PrestigeBreakingFormationLabel");
    prestigeBreakingFormationLabel.Text = String.Format("You lost {0} prestige from breaking formation.", playerComponent.PrestigeFrom(PrestigeSource.BREAKING_FORMATION));  
    
    Label prestigeFleeingLabel = GetNode<Label>("PrestigeFleeingLabel");
    prestigeFleeingLabel.Text = String.Format("You lost {0} prestige from fleeing the battlefield.", playerComponent.PrestigeFrom(PrestigeSource.FLEEING));  
  }

  private void OnMainMenuBttonPressed() {
    this._state.WriteToFile();
    ((SceneManager)GetNode("/root/SceneManager")).ExitToMainMenu();
  }

  private void OnSaveAndQuitButtonPressed() {
    this._state.WriteToFile();
    GetTree().Quit();
  }
}
