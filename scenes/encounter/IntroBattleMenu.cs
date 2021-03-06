using Godot;
using MTW7DRL2021.scenes.singletons;
using System;

namespace MTW7DRL2021.scenes.encounter {
  public class IntroBattleMenu : VBoxContainer {
    
    private Button _closeButton;

    public override void _Ready() {
      this._closeButton = GetNode<Button>("Button");
      _closeButton.Connect("pressed", this, nameof(OnCloseButtonPressed));
      _closeButton.Connect("tree_entered", this, nameof(OnTreeEntered));
    }

    private void OnTreeEntered() {
      _closeButton.GrabFocus();
    }

    private void OnCloseButtonPressed() {
      var sceneManager = (SceneManager)GetNode("/root/SceneManager");
      sceneManager.ReturnToPreviousScene();
    }
  }
}