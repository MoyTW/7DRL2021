using Godot;
using MTW7DRL2021.library;
using MTW7DRL2021.library.encounter;
using MTW7DRL2021.resources.gamedata;
using MTW7DRL2021.scenes.components;
using MTW7DRL2021.scenes.components.AI;
using MTW7DRL2021.scenes.entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace MTW7DRL2021.scenes.encounter.state {

  public class UnitAtLanePosition {
    [JsonInclude] public int LaneIdx { get; set; }
    [JsonInclude] public string UnitId { get; set; }
    [JsonInclude] public int LanePosition { get; set; }

    public UnitAtLanePosition(int laneIdx, string unitId, int lanePosition) {
      this.LaneIdx = laneIdx;
      this.UnitId = unitId;
      this.LanePosition = lanePosition;
    }
  }

  public class Lane {
    [JsonInclude] public int LaneIdx { get; private set; }
    [JsonInclude] public EncounterPosition LaneCenter { get; private set; }
    [JsonInclude] public Dictionary<FactionName, List<UnitAtLanePosition>> _UnitsForFaction { get; private set; }

    public Lane(int laneIdx, EncounterPosition laneCenter) {
      this.LaneIdx = laneIdx;
      this.LaneCenter = laneCenter;
      this._UnitsForFaction = new Dictionary<FactionName, List<UnitAtLanePosition>>() {
        { FactionName.ENEMY, new List<UnitAtLanePosition>() },
        { FactionName.NEUTRAL, new List<UnitAtLanePosition>() },
        { FactionName.PLAYER, new List<UnitAtLanePosition>() }
      };
    }

    public bool IsOnFlank(FactionName faction, Flank flank) {
      return true;
    }

    public List<UnitAtLanePosition> UnitsForFaction(FactionName faction) {
      return this._UnitsForFaction[faction];
    }

    public void RegisterUnitAtPosition(Unit unit, int lanePosition) {
      this._UnitsForFaction[unit.UnitFaction].Add(new UnitAtLanePosition(this.LaneIdx, unit.UnitId, lanePosition));
    }

    // Line indices are "lower towards enemy" - 0th = skirmishers, first = hastatus, third = triarius, highest = reserves
    public EncounterPosition PositionFor(FormationFacing facing, int line, int interval = 15) {
      // The facing has to be swapped in order for the position to work - an army FACING north should be DEPLOYED south
      return AIUtils.RotateAndProject(LaneCenter, 0, -interval * line - interval / 5, facing.Opposite());
    }
  }

  public class DeploymentInfo {
    [JsonInclude] public FactionName Attacker { get; private set; }
    [JsonInclude] public FactionName Defender { get; private set; }
    [JsonInclude] public FormationFacing AttackerFacing { get; private set; }
    [JsonInclude] public FormationFacing DefenderFacing { get; private set; }

    [JsonInclude] public FormationFacing PlayerFacing { get; private set; }
    [JsonInclude] public FormationFacing EnemyFacing { get; private set; }

    [JsonInclude] public EncounterPosition CenterPos { get; private set; }
    [JsonInclude] public int NumLanes { get; private set; }
    [JsonInclude] public List<Lane> Lanes { get; private set; }

    public static DeploymentInfo Create(int width, int height, Random seededRand, int numLanes) {
      var info = new DeploymentInfo();

      if (seededRand.Next(2) == 0) {
        info.Attacker = FactionName.PLAYER;
        info.Defender = FactionName.ENEMY;
      } else {
        info.Attacker = FactionName.ENEMY;
        info.Defender = FactionName.PLAYER;
      }
      info.AttackerFacing = (FormationFacing)seededRand.Next(4);
      info.DefenderFacing = info.AttackerFacing.Opposite();
      GD.Print("Attacking faction is: ", info.Attacker);

      if (info.Attacker == FactionName.PLAYER) {
        info.PlayerFacing = info.AttackerFacing;
        info.EnemyFacing = info.DefenderFacing;
      } else {
        info.PlayerFacing = info.DefenderFacing;
        info.EnemyFacing = info.AttackerFacing;
      }

      var interval = 20;
      info.CenterPos = new EncounterPosition(width / 2, height / 2);
      info.NumLanes = numLanes;
      info.Lanes = new List<Lane>();
      var leftX = -(Mathf.FloorToInt(info.NumLanes / 2) * interval);
      if (numLanes % 2 == 0) {
        leftX = -(info.NumLanes / 2 * interval) + interval / 2;
      }
      for (int i = 0; i < info.NumLanes; i++) {
        var laneX = leftX + i * interval;
        // Lanes are laid out from the perspective of the attacker
        var laneCenterPos = AIUtils.RotateAndProject(info.CenterPos, laneX, 0, info.AttackerFacing);
        info.Lanes.Add(new Lane(i, laneCenterPos));
      }
      return info;
    }
  }
}