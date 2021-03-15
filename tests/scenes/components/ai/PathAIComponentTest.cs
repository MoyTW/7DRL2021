using System.Collections.Generic;
using System.Text.Json;
using MTW7DRL2021.library.encounter;
using MTW7DRL2021.scenes.components.AI;
using Xunit;
using Xunit.Abstractions;

namespace MTW7DRL2021.tests.scenes.components.AI {

  public class PathAIComponentTest {

    private readonly ITestOutputHelper _output;

    public PathAIComponentTest(ITestOutputHelper output) {
      this._output = output;
    }
  }
}