using Godot;
using SpaceDodgeRL.scenes.encounter.state;
using SpaceDodgeRL.scenes.singletons;

public class VictoryMenu : VBoxContainer {

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
