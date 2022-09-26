using System;

namespace Omegasis.SaveAnywhere.Framework
{
  public class SchedulePathInfo
  {
    public int timeToGoTo;
    public int endX;
    public int endY;
    public string endMap;
    public int endDirection;
    public string endBehavior;
    public string endMessage;

    public SchedulePathInfo()
    {
    }

    public SchedulePathInfo(string RawData)
    {
      string[] strArray = RawData.Split(' ');
      try
      {
        this.timeToGoTo = Convert.ToInt32(strArray[0]);
      }
      catch (Exception ex)
      {
        return;
      }
      this.endMap = strArray[1];
      this.endX = Convert.ToInt32(strArray[2]);
      this.endY = Convert.ToInt32(strArray[3]);
      this.endDirection = Convert.ToInt32(strArray[4]);
      if (strArray.Length >= 6 && strArray[5][0] == '"')
        this.endMessage = strArray[5].Substring(strArray[5].IndexOf('"'));
      if (strArray.Length < 7)
        return;
      if (strArray[5][0] == '"')
        this.endBehavior = strArray[5];
      else
        this.endMessage = strArray[6].Substring(strArray[6].IndexOf('"'));
    }
  }
}
