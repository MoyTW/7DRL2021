using System.Text.Json;
using MTW7DRL2021.scenes.components;
using Xunit;
using Xunit.Abstractions;

namespace MTW7DRL2021.tests.scenes.components {

  public class ActionTimeComponentTest {

    private readonly ITestOutputHelper _output;

    public ActionTimeComponentTest(ITestOutputHelper output) {
      this._output = output;
    }

    [Fact]
    public void IncludesEntityGroup() {
      var component = ActionTimeComponent.Create(0, 0);
      JsonElement deserialized = JsonSerializer.Deserialize<JsonElement>(component.Save());
      Assert.Equal(ActionTimeComponent.ENTITY_GROUP, deserialized.GetProperty("EntityGroup").GetString());
    }

    [Fact]
    public void SerializesAndDeserializesCorrectly() {
      var component = ActionTimeComponent.Create(37, 57);
      string saved = component.Save();

      var newComponent = ActionTimeComponent.Create(saved);

      Assert.Equal(component.NextTurnAtTick, newComponent.NextTurnAtTick);
      Assert.Equal(component.LastTurnAtTick, newComponent.LastTurnAtTick);
    }
  }
}