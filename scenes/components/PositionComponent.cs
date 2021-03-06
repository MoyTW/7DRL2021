using Godot;
using MTW7DRL2021.library.encounter;
using MTW7DRL2021.scenes.encounter.state;
using MTW7DRL2021.scenes.entities;
using MTW7DRL2021.scenes.singletons;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MTW7DRL2021.scenes.components {

  [JsonConverter(typeof(PositionConverter))]
  public class PositionComponent : Node, Component {
    private static PackedScene _scenePrefab = GD.Load<PackedScene>("res://scenes/components/PositionComponent.tscn");

    public static readonly string ENTITY_GROUP = "POSITION_COMPONENT_GROUP";
    public string EntityGroup => ENTITY_GROUP;

    // TODO: Don't put this here
    public const int START_X = 16;
    public const int START_Y = 16;
    public const int STEP_X = 32;
    public const int STEP_Y = 32;

    public Texture SpriteTexture { get => GetNode<Sprite>("Sprite").Texture; }
    private GameSettings _gameSettings;
    private GameSettings GameSettings { get {
      if (this._gameSettings == null) {
        this._gameSettings = this.GetNode<GameSettings>("/root/GameSettings");
      }
      return this._gameSettings;
    } }

    private EncounterPosition _encounterPosition = new EncounterPosition(int.MinValue, int.MinValue);
    public EncounterPosition EncounterPosition { get => _encounterPosition; }

    public bool IsAnimating { get {
      var encounterPosition = IndexToVector(this.EncounterPosition.X, this.EncounterPosition.Y);
      return GetNode<Sprite>("Sprite").Position != encounterPosition || GetNode<AnimatedSprite>("ExplosionSprite").Visible == true;
    } }

    public static PositionComponent Create(EncounterPosition position, string texturePath, int zIndex, bool visible) {
      var component = _scenePrefab.Instance() as PositionComponent;

      component._encounterPosition = position;
      var sprite = component.GetNode<Sprite>("Sprite");
      sprite.Position = IndexToVector(position.X, position.Y);
      sprite.Texture = GD.Load<Texture>(texturePath);
      sprite.ZIndex = zIndex;

      var explosionSprite = component.GetNode<AnimatedSprite>("ExplosionSprite");
      explosionSprite.Connect("animation_finished", component, nameof(OnExplosionAnimationFinished));
      var timer = component.GetNode<Timer>("SpeechBubbleTimer");
      timer.Connect("timeout", component, nameof(OnSpeechBubbleTimerTimeout));

      if (visible) {
        component.Show();
      } else {
        component.Hide();
      }

      return component;
    }

    public static PositionComponent Create(string saveData) {
      var loaded = JsonSerializer.Deserialize<SaveData>(saveData);
      return PositionComponent.Create(loaded.EncounterPosition, loaded.TexturePath, loaded.ZIndex, loaded.Visible);
    }

    public void SetEncounterPosition(EncounterPosition position, bool show) {
      var dx = position.X - _encounterPosition.X;
      var dy = position.Y - _encounterPosition.Y;
      //RotateSpriteTowards(dx, dy); don't rotate anymore

      _encounterPosition = position;
      Tween(IndexToVector(position.X, position.Y));

      this.Show();
      
    }

    public void Show() {
      this.GetNode<Sprite>("Sprite").Show();
    }

    public void Hide() {
      this.GetNode<Sprite>("Sprite").Hide();
    }

    public void RotateSpriteTowards(int dx, int dy) {
      // Invert y
      dy = 0 - dy;
      var sprite = this.GetNode<Sprite>("Sprite");
      if (dx == 0 && dy > 0) {
        sprite.RotationDegrees = 0;
      } else if (dx > 0 && dy > 0) {
        sprite.RotationDegrees = 45;
      } else if (dx > 0 && dy == 0) {
        sprite.RotationDegrees = 90;
      } else if (dx > 0 && dy < 0) {
        sprite.RotationDegrees = 135;
      } else if (dx == 0 && dy < 0) {
        sprite.RotationDegrees = 180;
      } else if (dx < 0 && dy < 0) {
        sprite.RotationDegrees = 225;
      } else if (dx < 0 && dy == 0) {
        sprite.RotationDegrees = 270;
      } else if (dx < 0 && dy > 0) {
        sprite.RotationDegrees = 315;
      }
    }

    // TODO: Attempt to sync this up with the turn time?
    // TODO: More than one animation
    public void PlayExplosion() {
      var tween = this.GetNode<Tween>("Tween");
      if (tween.IsActive()) {
        tween.Connect("tween_completed", this, nameof(OnTweenAllCompletedQueueExplosion));
      } else {
        var explosionSprite = this.GetNode<AnimatedSprite>("ExplosionSprite");
        explosionSprite.Position = IndexToVector(this.EncounterPosition.X, this.EncounterPosition.Y);
        explosionSprite.Visible = true;
        explosionSprite.Play();
      }
    }

    public void RestartTween() {
      var tween = GetNode<Tween>("Tween");
      var sprite = GetNode<Sprite>("Sprite");
      var encounterPosition = IndexToVector(this.EncounterPosition.X, this.EncounterPosition.Y);
      tween.InterpolateProperty(sprite, "position", sprite.Position, encounterPosition, this.GameSettings.TurnTimeMs / 1000f);
      tween.Start();
    }

    private void Tween(Vector2 newPosition) {
      var tween = GetNode<Tween>("Tween");
      var sprite = GetNode<Sprite>("Sprite");
      tween.InterpolateProperty(sprite, "position", sprite.Position, newPosition, this.GameSettings.TurnTimeMs / 1000f);
      tween.Start();
    }

    private void OnTweenAllCompletedQueueExplosion(Godot.Object o, Godot.NodePath path) {
      this.GetNode<Sprite>("Sprite").Visible = false;
      var explosionSprite = this.GetNode<AnimatedSprite>("ExplosionSprite");
      explosionSprite.Position = IndexToVector(this.EncounterPosition.X, this.EncounterPosition.Y);
      explosionSprite.Visible = true;
      explosionSprite.Play();
    }

    private void OnExplosionAnimationFinished() {
      var explosionSprite = this.GetNode<AnimatedSprite>("ExplosionSprite");
      explosionSprite.Visible = false;
    }

    public void PlaySpeechBubble(string speech) {
      var timer = GetNode<Timer>("SpeechBubbleTimer");
      if (timer.IsStopped()) {
        var bubble = GetNode<NinePatchRect>("Sprite/Node2D/NinePatchRect");
        var speechLabel = GetNode<Label>("Sprite/Node2D/NinePatchRect/SpeechLabel");
        speechLabel.Text = speech;
        var newSize = new Vector2(10, speechLabel.RectSize.y);
        speechLabel.RectSize = newSize;
        bubble.RectMinSize = new Vector2(speech.Length * 10 + 25, bubble.RectSize.y);
        bubble.Show();
        timer.Start(1);
      }
    }

    private void OnSpeechBubbleTimerTimeout() {
      var bubble = GetNode<NinePatchRect>("Sprite/Node2D/NinePatchRect");
      bubble.Hide();
      var timer = GetNode<Timer>("SpeechBubbleTimer");
      timer.Stop();
    }

    public static EncounterPosition VectorToIndex(float x, float y) {
      return new EncounterPosition((int)(x / STEP_X), (int)(y / STEP_Y));
    }

    public static Vector2 IndexToVector(int x, int y) {
      return new Vector2(START_X + STEP_X * x, START_Y + STEP_Y * y);
    }

    public class SaveData {
      public string EntityGroup { get; set; }
      public EncounterPosition EncounterPosition { get; set; }
      public string TexturePath { get; set; }
      public int ZIndex { get; set; }
      public bool Visible { get; set; }

      public SaveData() { }

      public SaveData(PositionComponent component) {
        var sprite = component.GetNode<Sprite>("Sprite");

        this.EntityGroup = PositionComponent.ENTITY_GROUP;
        this.EncounterPosition = component.EncounterPosition;
        this.TexturePath = sprite.Texture.ResourcePath;
        this.ZIndex = sprite.ZIndex;
        this.Visible = sprite.Visible;
      }
    }

    public string Save() {
      return JsonSerializer.Serialize(new SaveData(this));
    }

    public void NotifyAttached(Entity parent) { }

    public void NotifyDetached(Entity parent) { }
  }

  public class PositionConverter : JsonConverter<PositionComponent> {
    public override PositionComponent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
      using (var doc = JsonDocument.ParseValue(ref reader)) {
        return PositionComponent.Create(doc.RootElement.GetRawText());
      }
    }

    public override void Write(Utf8JsonWriter writer, PositionComponent value, JsonSerializerOptions options) {
      JsonSerializer.Serialize(writer, new PositionComponent.SaveData(value), options);
    }
  }
}