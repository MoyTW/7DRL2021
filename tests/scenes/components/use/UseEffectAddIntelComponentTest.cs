using System.Text.Json;
using MTW7DRL2021.scenes.components.use;
using Xunit;
using Xunit.Abstractions;

namespace MTW7DRL2021.tests.scenes.components.use {

  public class UseEffectAddIntelComponentTest {

    private readonly ITestOutputHelper _output;

    public UseEffectAddIntelComponentTest(ITestOutputHelper output) {
      this._output = output;
    }

    [Fact]
    public void IncludesEntityGroup() {
      var component = UseEffectAddIntelComponent.Create(0);
      JsonElement deserialized = JsonSerializer.Deserialize<JsonElement>(component.Save());
      Assert.Equal(UseEffectAddIntelComponent.ENTITY_GROUP, deserialized.GetProperty("EntityGroup").GetString());
    }

    [Fact]
    public void SerializesAndDeserializesCorrectly() {
      var component = UseEffectAddIntelComponent.Create(4);
      string saved = component.Save();

      var newComponent = UseEffectAddIntelComponent.Create(saved);

      Assert.Equal(component.TargetDungeonLevel, newComponent.TargetDungeonLevel);
    }
  }
}