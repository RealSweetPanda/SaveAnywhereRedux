using Microsoft.Xna.Framework.Content;
using Netcode;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Omegasis.SaveAnywhere.Framework
{
  public static class NPCExtensions
  {
    public static void fillInSchedule(this NPC npc)
    {
      if (npc.Schedule == null)
        return;
      IDictionary<string, string> rawSchedule = NPCExtensions.GetRawSchedule(((Character) npc).Name);
      if (rawSchedule == null)
        return;
      string scheduleKey = NPCExtensions.GetScheduleKey(npc);
      string str;
      rawSchedule.TryGetValue(scheduleKey, out str);
      if (string.IsNullOrEmpty(str))
        return;
      string[] strArray1 = str.Split('/');
      SortedDictionary<int, SchedulePathInfo> source = new SortedDictionary<int, SchedulePathInfo>();
      for (int index = 0; index < strArray1.Length; ++index)
      {
        string[] strArray2 = strArray1[index].Split(' ');
        if (strArray2[0].Equals("GOTO"))
        {
          rawSchedule.TryGetValue(strArray2[1], out str);
          strArray1 = str.Split('/');
          index = -1;
        }
        SchedulePathInfo schedulePathInfo = new SchedulePathInfo(strArray1[index]);
        if (schedulePathInfo.timeToGoTo != 0)
          source.Add(schedulePathInfo.timeToGoTo, schedulePathInfo);
      }
      int index1 = 0;
      List<KeyValuePair<int, SchedulePathInfo>> list = source.ToList<KeyValuePair<int, SchedulePathInfo>>();
      list.OrderBy<KeyValuePair<int, SchedulePathInfo>, int>((Func<KeyValuePair<int, SchedulePathInfo>, int>) (i => i.Key));
      KeyValuePair<int, SchedulePathInfo> keyValuePair;
      for (int key1 = 600; key1 <= 2600; key1 += 10)
      {
        if (index1 >= list.Count && !source.ContainsKey(key1))
        {
          SortedDictionary<int, SchedulePathInfo> sortedDictionary = source;
          int key2 = key1;
          keyValuePair = list[list.Count - 1];
          SchedulePathInfo schedulePathInfo = keyValuePair.Value;
          sortedDictionary.Add(key2, schedulePathInfo);
        }
        else if (index1 == list.Count - 1)
        {
          if (!source.ContainsKey(key1))
          {
            SortedDictionary<int, SchedulePathInfo> sortedDictionary = source;
            int key3 = key1;
            keyValuePair = list[index1];
            SchedulePathInfo schedulePathInfo = keyValuePair.Value;
            sortedDictionary.Add(key3, schedulePathInfo);
          }
        }
        else
        {
          int num = key1;
          keyValuePair = list[index1 + 1];
          int key4 = keyValuePair.Key;
          if (num == key4)
            ++index1;
          else if (!source.ContainsKey(key1))
          {
            SortedDictionary<int, SchedulePathInfo> sortedDictionary = source;
            int key5 = key1;
            keyValuePair = list[index1];
            SchedulePathInfo schedulePathInfo = keyValuePair.Value;
            sortedDictionary.Add(key5, schedulePathInfo);
          }
        }
      }
      SchedulePathInfo schedulePathInfo1 = source[Game1.timeOfDay];
      SchedulePathDescription schedulePathDescription = Omegasis.SaveAnywhere.SaveAnywhere.ModHelper.Reflection.GetMethod((object) npc, "pathfindToNextScheduleLocation", true).Invoke<SchedulePathDescription>(new object[9]
      {
        (object) ((Character) npc).currentLocation.Name,
        (object) ((Character) npc).getTileX(),
        (object) ((Character) npc).getTileY(),
        (object) schedulePathInfo1.endMap,
        (object) schedulePathInfo1.endX,
        (object) schedulePathInfo1.endY,
        (object) schedulePathInfo1.endDirection,
        (object) schedulePathInfo1.endBehavior,
        (object) schedulePathInfo1.endMessage
      });
      npc.DirectionsToNewLocation = schedulePathDescription;
      ((Character) npc).controller = new PathFindController(npc.DirectionsToNewLocation.route, (Character) npc, Utility.getGameLocationOfCharacter(npc))
      {
        finalFacingDirection = npc.DirectionsToNewLocation.facingDirection,
        endBehaviorFunction = (PathFindController.endBehavior) null
      };
    }

    private static IDictionary<string, string> GetRawSchedule(string npcName)
    {
      try
      {
        return (IDictionary<string, string>) ((ContentManager) Game1.content).Load<Dictionary<string, string>>("Characters\\schedules\\" + npcName);
      }
      catch
      {
        return (IDictionary<string, string>) null;
      }
    }

    private static string GetScheduleKey(NPC npc)
    {
      string str1 = Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth);
      string str2 = "";
      if (((Character) npc).Name.Equals("Penny") && (str1.Equals("Tue") || str1.Equals("Wed") || str1.Equals("Fri")) || ((Character) npc).Name.Equals("Maru") && (str1.Equals("Tue") || str1.Equals("Thu")) || ((Character) npc).Name.Equals("Harvey") && (str1.Equals("Tue") || str1.Equals("Thu")))
        str2 = "marriageJob";
      if (!Game1.isRaining && npc.hasMasterScheduleEntry("marriage_" + Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth)))
        str2 = "marriage_" + Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth);
      if (npc.hasMasterScheduleEntry(Game1.currentSeason + "_" + Game1.dayOfMonth.ToString()))
        str2 = Game1.currentSeason + "_" + Game1.dayOfMonth.ToString();
      int playerFriendshipLevel1 = Utility.GetAllPlayerFriendshipLevel(npc);
      if (playerFriendshipLevel1 >= 0)
        playerFriendshipLevel1 /= 250;
      for (; playerFriendshipLevel1 > 0; --playerFriendshipLevel1)
      {
        if (npc.hasMasterScheduleEntry(Game1.dayOfMonth.ToString() + "_" + playerFriendshipLevel1.ToString()))
          str2 = Game1.dayOfMonth.ToString() + "_" + playerFriendshipLevel1.ToString();
      }
      if (npc.hasMasterScheduleEntry(Game1.dayOfMonth.ToString()))
        str2 = Game1.dayOfMonth.ToString();
      if (((Character) npc).Name.Equals("Pam") && ((NetList<string, NetString>) Game1.player.mailReceived).Contains("ccVault"))
        str2 = "bus";
      if (Game1.isRaining)
      {
        if (Game1.random.NextDouble() < 0.5 && npc.hasMasterScheduleEntry("rain2"))
          str2 = "rain2";
        if (npc.hasMasterScheduleEntry("rain"))
          str2 = "rain";
      }
      List<string> values = new List<string>()
      {
        Game1.currentSeason,
        Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth)
      };
      int playerFriendshipLevel2 = Utility.GetAllPlayerFriendshipLevel(npc);
      if (playerFriendshipLevel2 >= 0)
        playerFriendshipLevel2 /= 250;
      while (playerFriendshipLevel2 > 0)
      {
        values.Add(string.Empty + playerFriendshipLevel2.ToString());
        if (npc.hasMasterScheduleEntry(string.Join("_", (IEnumerable<string>) values)))
        {
          str2 = string.Join("_", (IEnumerable<string>) values);
          break;
        }
        --playerFriendshipLevel2;
        values.RemoveAt(values.Count - 1);
      }
      if (npc.hasMasterScheduleEntry(string.Join("_", (IEnumerable<string>) values)))
        str2 = string.Join("_", (IEnumerable<string>) values);
      if (npc.hasMasterScheduleEntry(Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth)))
        str2 = Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth);
      if (npc.hasMasterScheduleEntry(Game1.currentSeason))
        str2 = Game1.currentSeason;
      if (npc.hasMasterScheduleEntry("spring_" + Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth)))
        str2 = "spring_" + Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth);
      values.RemoveAt(values.Count - 1);
      values.Add("spring");
      int playerFriendshipLevel3 = Utility.GetAllPlayerFriendshipLevel(npc);
      if (playerFriendshipLevel3 >= 0)
        playerFriendshipLevel3 /= 250;
      while (playerFriendshipLevel3 > 0)
      {
        values.Add(string.Empty + playerFriendshipLevel3.ToString());
        if (npc.hasMasterScheduleEntry(string.Join("_", (IEnumerable<string>) values)))
          str2 = string.Join("_", (IEnumerable<string>) values);
        --playerFriendshipLevel3;
        values.RemoveAt(values.Count - 1);
      }
      return !npc.hasMasterScheduleEntry("spring") ? "" : "spring";
    }
  }
}
