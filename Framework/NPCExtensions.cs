using System;
using System.Collections.Generic;
using System.Linq;
using StardewValley;

namespace SaveAnywhere.Framework {
    public static class NPCExtensions {
        public static void fillInSchedule(this NPC npc) {
            if (npc.Schedule == null)
                return;
            var rawSchedule = GetRawSchedule(npc.Name);
            if (rawSchedule == null)
                return;
            var scheduleKey = GetScheduleKey(npc);
            string str;
            rawSchedule.TryGetValue(scheduleKey, out str);
            if (string.IsNullOrEmpty(str))
                return;
            var strArray1 = str.Split('/');
            var source = new SortedDictionary<int, SchedulePathInfo>();
            for (var index = 0; index < strArray1.Length; ++index) {
                var strArray2 = strArray1[index].Split(' ');
                if (strArray2[0].Equals("GOTO")) {
                    rawSchedule.TryGetValue(strArray2[1], out str);
                    strArray1 = str.Split('/');
                    index = -1;
                }

                var schedulePathInfo = new SchedulePathInfo(strArray1[index]);
                if (schedulePathInfo.timeToGoTo != 0)
                    source.Add(schedulePathInfo.timeToGoTo, schedulePathInfo);
            }

            var index1 = 0;
            var list = source.ToList();
            list.OrderBy((Func<KeyValuePair<int, SchedulePathInfo>, int>) (i => i.Key));
            KeyValuePair<int, SchedulePathInfo> keyValuePair;
            for (var key1 = 600; key1 <= 2600; key1 += 10)
                if (index1 >= list.Count && !source.ContainsKey(key1)) {
                    var sortedDictionary = source;
                    var key2 = key1;
                    keyValuePair = list[list.Count - 1];
                    var schedulePathInfo = keyValuePair.Value;
                    sortedDictionary.Add(key2, schedulePathInfo);
                }
                else if (index1 == list.Count - 1) {
                    if (!source.ContainsKey(key1)) {
                        var sortedDictionary = source;
                        var key3 = key1;
                        keyValuePair = list[index1];
                        var schedulePathInfo = keyValuePair.Value;
                        sortedDictionary.Add(key3, schedulePathInfo);
                    }
                }
                else {
                    var num = key1;
                    keyValuePair = list[index1 + 1];
                    var key4 = keyValuePair.Key;
                    if (num == key4) {
                        ++index1;
                    }
                    else if (!source.ContainsKey(key1)) {
                        var sortedDictionary = source;
                        var key5 = key1;
                        keyValuePair = list[index1];
                        var schedulePathInfo = keyValuePair.Value;
                        sortedDictionary.Add(key5, schedulePathInfo);
                    }
                }

            var schedulePathInfo1 = source[Game1.timeOfDay];
            var schedulePathDescription = SaveAnywhere.ModHelper.Reflection
                .GetMethod(npc, "pathfindToNextScheduleLocation").Invoke<SchedulePathDescription>(
                    npc.currentLocation.Name, npc.getTileX(), npc.getTileY(), schedulePathInfo1.endMap,
                    schedulePathInfo1.endX, schedulePathInfo1.endY, schedulePathInfo1.endDirection,
                    schedulePathInfo1.endBehavior, schedulePathInfo1.endMessage);
            npc.DirectionsToNewLocation = schedulePathDescription;
            npc.controller = new PathFindController(npc.DirectionsToNewLocation.route, npc,
                Utility.getGameLocationOfCharacter(npc)) {
                finalFacingDirection = npc.DirectionsToNewLocation.facingDirection,
                endBehaviorFunction = null
            };
        }

        private static IDictionary<string, string> GetRawSchedule(string npcName) {
            try {
                return Game1.content.Load<Dictionary<string, string>>("Characters\\schedules\\" + npcName);
            }
            catch {
                return null;
            }
        }

        private static string GetScheduleKey(NPC npc) {
            var str1 = Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth);
            var str2 = "";
            if ((npc.Name.Equals("Penny") && (str1.Equals("Tue") || str1.Equals("Wed") || str1.Equals("Fri"))) ||
                (npc.Name.Equals("Maru") && (str1.Equals("Tue") || str1.Equals("Thu"))) ||
                (npc.Name.Equals("Harvey") && (str1.Equals("Tue") || str1.Equals("Thu"))))
                str2 = "marriageJob";
            if (!Game1.isRaining &&
                npc.hasMasterScheduleEntry("marriage_" + Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth)))
                str2 = "marriage_" + Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth);
            if (npc.hasMasterScheduleEntry(Game1.currentSeason + "_" + Game1.dayOfMonth))
                str2 = Game1.currentSeason + "_" + Game1.dayOfMonth;
            var playerFriendshipLevel1 = Utility.GetAllPlayerFriendshipLevel(npc);
            if (playerFriendshipLevel1 >= 0)
                playerFriendshipLevel1 /= 250;
            for (; playerFriendshipLevel1 > 0; --playerFriendshipLevel1)
                if (npc.hasMasterScheduleEntry(Game1.dayOfMonth + "_" + playerFriendshipLevel1))
                    str2 = Game1.dayOfMonth + "_" + playerFriendshipLevel1;
            if (npc.hasMasterScheduleEntry(Game1.dayOfMonth.ToString()))
                str2 = Game1.dayOfMonth.ToString();
            if (npc.Name.Equals("Pam") && Game1.player.mailReceived.Contains("ccVault"))
                str2 = "bus";
            if (Game1.isRaining) {
                if (Game1.random.NextDouble() < 0.5 && npc.hasMasterScheduleEntry("rain2"))
                    str2 = "rain2";
                if (npc.hasMasterScheduleEntry("rain"))
                    str2 = "rain";
            }

            var values = new List<string> {
                Game1.currentSeason,
                Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth)
            };
            var playerFriendshipLevel2 = Utility.GetAllPlayerFriendshipLevel(npc);
            if (playerFriendshipLevel2 >= 0)
                playerFriendshipLevel2 /= 250;
            while (playerFriendshipLevel2 > 0) {
                values.Add(string.Empty + playerFriendshipLevel2);
                if (npc.hasMasterScheduleEntry(string.Join("_", values))) {
                    str2 = string.Join("_", values);
                    break;
                }

                --playerFriendshipLevel2;
                values.RemoveAt(values.Count - 1);
            }

            if (npc.hasMasterScheduleEntry(string.Join("_", values)))
                str2 = string.Join("_", values);
            if (npc.hasMasterScheduleEntry(Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth)))
                str2 = Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth);
            if (npc.hasMasterScheduleEntry(Game1.currentSeason))
                str2 = Game1.currentSeason;
            if (npc.hasMasterScheduleEntry("spring_" + Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth)))
                str2 = "spring_" + Game1.shortDayNameFromDayOfSeason(Game1.dayOfMonth);
            values.RemoveAt(values.Count - 1);
            values.Add("spring");
            var playerFriendshipLevel3 = Utility.GetAllPlayerFriendshipLevel(npc);
            if (playerFriendshipLevel3 >= 0)
                playerFriendshipLevel3 /= 250;
            while (playerFriendshipLevel3 > 0) {
                values.Add(string.Empty + playerFriendshipLevel3);
                if (npc.hasMasterScheduleEntry(string.Join("_", values)))
                    str2 = string.Join("_", values);
                --playerFriendshipLevel3;
                values.RemoveAt(values.Count - 1);
            }

            return !npc.hasMasterScheduleEntry("spring") ? "" : "spring";
        }
    }
}