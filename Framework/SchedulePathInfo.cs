using System;

namespace SaveAnywhere.Framework {
    public class SchedulePathInfo {
        public string endBehavior;
        public int endDirection;
        public string endMap;
        public string endMessage;
        public int endX;
        public int endY;
        public int timeToGoTo;

        public SchedulePathInfo() { }

        public SchedulePathInfo(string RawData) {
            var strArray = RawData.Split(' ');
            try {
                timeToGoTo = Convert.ToInt32(strArray[0]);
            }
            catch (Exception ex) {
                return;
            }

            endMap = strArray[1];
            endX = Convert.ToInt32(strArray[2]);
            endY = Convert.ToInt32(strArray[3]);
            endDirection = Convert.ToInt32(strArray[4]);
            if (strArray.Length >= 6 && strArray[5][0] == '"')
                endMessage = strArray[5].Substring(strArray[5].IndexOf('"'));
            if (strArray.Length < 7)
                return;
            if (strArray[5][0] == '"')
                endBehavior = strArray[5];
            else
                endMessage = strArray[6].Substring(strArray[6].IndexOf('"'));
        }
    }
}