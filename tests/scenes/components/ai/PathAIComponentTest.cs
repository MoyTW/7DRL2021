using System.Collections.Generic;
using System.Text.Json;
using SpaceDodgeRL.library.encounter;
using SpaceDodgeRL.scenes.components.AI;
using Xunit;
using Xunit.Abstractions;

namespace SpaceDodgeRL.tests.scenes.components.AI {

  public class PathAIComponentTest {

    private readonly ITestOutputHelper _output;

    public PathAIComponentTest(ITestOutputHelper output) {
      this._output = output;
    }
  }
}