using Godot;
using MTW7DRL2021.scenes.components;
using MTW7DRL2021.scenes.encounter;
using MTW7DRL2021.scenes.encounter.state;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MTW7DRL2021.scenes.singletons {

  public class SceneManager : Node {

    private Viewport root;
    private List<Node> sceneStack;

    private IntroFormUpMenu _introFormUpMenu;
    private IntroBattleMenu _introBattleMenu;

    private CreditsMenu _creditsMenu;
    private DefeatMenu _defeatMenu;
    private EscapeMenu _escapeMenu;
    private HelpMenu _helpMenu;
    private SettingsMenu _settingsMenu;
    private VictoryMenu _victoryMenu;

    public override void _Ready() {
      root = GetTree().Root;
      sceneStack = new List<Node>();

      _introFormUpMenu = GD.Load<PackedScene>("res://scenes/encounter/IntroFormUpMenu.tscn").Instance() as IntroFormUpMenu;
      _introBattleMenu = GD.Load<PackedScene>("res://scenes/encounter/IntroBattleMenu.tscn").Instance() as IntroBattleMenu;

      _creditsMenu = GD.Load<PackedScene>("res://scenes/CreditsMenu.tscn").Instance() as CreditsMenu;
      _defeatMenu = GD.Load<PackedScene>("res://scenes/encounter/DefeatMenu.tscn").Instance() as DefeatMenu;
      _escapeMenu = GD.Load<PackedScene>("res://scenes/encounter/EscapeMenu.tscn").Instance() as EscapeMenu;
      _helpMenu = GD.Load<PackedScene>("res://scenes/encounter/HelpMenu.tscn").Instance() as HelpMenu;
      _settingsMenu = GD.Load<PackedScene>("res://scenes/SettingsMenu.tscn").Instance() as SettingsMenu;
      _victoryMenu = GD.Load<PackedScene>("res://scenes/encounter/VictoryMenu.tscn").Instance() as VictoryMenu;
    }

    public void ShowIntroFormUpMenu() {
      CallDeferred(nameof(DeferredIntroFormUpMenu));
    }

    private void DeferredIntroFormUpMenu() {
      DeferredSwitchScene(_introFormUpMenu);
    }

    public void ShowIntroBattleMenu() {
      CallDeferred(nameof(DeferredIntroBattleMenu));
    }

    private void DeferredIntroBattleMenu() {
      DeferredSwitchScene(_introBattleMenu);
    }

    #region Credits Menu

    public void ShowCreditsMenu() {
      CallDeferred(nameof(DeferredShowCreditsMenu));
    }

    private void DeferredShowCreditsMenu() {
      DeferredSwitchScene(_creditsMenu);
      _creditsMenu.PrepMenu();
    }

    #endregion

    # region Defeat Menu

    public void ShowDefeatMenu(EncounterState state) {
      CallDeferred(nameof(DeferredShowDefeatMenu), state);
    }

    private void DeferredShowDefeatMenu(EncounterState state) {
      DeferredSwitchScene(_defeatMenu);
      _defeatMenu.PrepMenu(state);
    }

    #endregion

    #region Escape Menu

    public void ShowEscapeMenu(EncounterState state) {
      CallDeferred(nameof(DeferredShowEscapeMenu), state);
    }

    private void DeferredShowEscapeMenu(EncounterState state) {
      DeferredSwitchScene(_escapeMenu);
      _escapeMenu.PrepMenu(state);
    }

    #endregion

    #region Help Menu

    public void ShowHelpMenu() {
      CallDeferred(nameof(DeferredShowHelpMenu));
    }

    private void DeferredShowHelpMenu() {
      DeferredSwitchScene(_helpMenu);
      _helpMenu.PrepMenu();
    }

    #endregion

    #region EncounterScene

    public void ShowEncounterScene(EncounterScene scene) {
      CallDeferred(nameof(DeferredShowEncounterScene), scene);
    }

    private void DeferredShowEncounterScene(EncounterScene scene) {
      DeferredSwitchScene(scene);
    }

    #endregion

    #region Settings Menu

    public void ShowSettingsMenu() {
      CallDeferred(nameof(DeferredShowSettingsMenu));
    }

    private void DeferredShowSettingsMenu() {
      DeferredSwitchScene(_settingsMenu);
      this._settingsMenu.SetFocus();
    }

    #endregion

    #region Victory Menu

    public void ShowVictoryMenu(EncounterState state) {
      CallDeferred(nameof(DeferredShowVictoryMenu), state);
    }

    private void DeferredShowVictoryMenu(EncounterState state) {
      DeferredSwitchScene(_victoryMenu);
      _victoryMenu.PrepMenu(state);
    }

    #endregion

    // Plumbing
    public void ReturnToPreviousScene() {
      CallDeferred(nameof(DeferredReturnToPreviousScene));
    }

    private void DeferredReturnToPreviousScene() {
      var previousScene = sceneStack[sceneStack.Count - 1];
      sceneStack.RemoveAt(sceneStack.Count - 1);
      DeferredSwitchScene(previousScene, true);
    }

    public void ExitToMainMenu() {
      CallDeferred(nameof(DeferredExitToMainMenu));
    }

    /**
     * Drops us back to the main menu and burns the scene stack.
     */
    private void DeferredExitToMainMenu() {
      var introMenuScene = sceneStack.Find(s => s is IntroMenuScene);
      DeferredSwitchScene(introMenuScene);
      sceneStack.Clear();
      (introMenuScene as IntroMenuScene).SetFocus();
    }

    private void DeferredSwitchScene(Node scene) {
      DeferredSwitchScene(scene, false);
    }

    /**
     * Swaps scenes, operating only on the last child node of root. Saves previous scenes in SceneStack.
     */
    private void DeferredSwitchScene(Node scene, bool previous) {
      var lastScene = root.GetChild(root.GetChildCount() - 1);
      if (!previous) {
        sceneStack.Add(lastScene);
      }
      root.RemoveChild(lastScene);
      root.AddChild(scene);
    }
  }
}