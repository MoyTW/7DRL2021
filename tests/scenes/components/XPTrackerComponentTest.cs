using System.Text.Json;
using SpaceDodgeRL.scenes.components;
using SpaceDodgeRL.scenes.entities;
using Xunit;
using Xunit.Abstractions;

namespace SpaceDodgeRL.tests.scenes.components {

  public class XPTrackerComponentTest {

    private readonly ITestOutputHelper _output;

    public XPTrackerComponentTest(ITestOutputHelper output) {
      this._output = output;
    }

    [Fact]
    public void IncludesEntityGroup() {
      var component = XPTrackerComponent.Create(0, 0);
      JsonElement deserialized = JsonSerializer.Deserialize<JsonElement>(component.Save());
      Assert.Equal(XPTrackerComponent.ENTITY_GROUP, deserialized.GetProperty("EntityGroup").GetString());
    }
  }
}