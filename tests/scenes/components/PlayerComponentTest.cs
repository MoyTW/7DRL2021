using System.Collections.Generic;
using System.Text.Json;
using SpaceDodgeRL.library.encounter;
using SpaceDodgeRL.scenes.components;
using SpaceDodgeRL.scenes.entities;
using Xunit;
using Xunit.Abstractions;

namespace SpaceDodgeRL.tests.scenes.components {

  public class PlayerComponentTest {

    private readonly ITestOutputHelper _output;

    public PlayerComponentTest(ITestOutputHelper output) {
      this._output = output;
    }

    [Fact]
    public void IncludesEntityGroup() {
      var component = PlayerComponent.Create();
      JsonElement deserialized = JsonSerializer.Deserialize<JsonElement>(component.Save());
      Assert.Equal(PlayerComponent.ENTITY_GROUP, deserialized.GetProperty("EntityGroup").GetString());
    }
  }
}