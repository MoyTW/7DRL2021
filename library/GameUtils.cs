using System;
using System.Collections.Generic;

namespace SpaceDodgeRL.library {

  public static class GameUtils {
    public static void Shuffle<T>(Random seededRand, List<T> target) {
        for (int i = target.Count - 1; i > 1; i--) {
        int rnd = seededRand.Next(i + 1);  

        T value = target[rnd];  
        target[rnd] = target[i];  
        target[i] = value;
      }
    }
  }
}