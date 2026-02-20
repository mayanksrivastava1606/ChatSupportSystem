using ChatSupportSystem.Models;

namespace ChatSupportSystem.Services;

public class ShiftManager
{
    /// <summary>
    /// Determines the current active shift based on UTC hour.
    /// Day: 06:00–14:00, Afternoon: 14:00–22:00, Night: 22:00–06:00
    /// </summary>
    public ShiftType GetCurrentShift(DateTime utcNow)
    {
        int hour = utcNow.Hour;
        return hour switch
        {
            >= 6 and < 14 => ShiftType.Day,
            >= 14 and < 22 => ShiftType.Afternoon,
            _ => ShiftType.Night
        };
    }

    /// <summary>
    /// Office hours = Day or Afternoon shift (06:00–22:00 UTC).
    /// </summary>
    public bool IsOfficeHours(DateTime utcNow)
    {
        var shift = GetCurrentShift(utcNow);
        return shift is ShiftType.Day or ShiftType.Afternoon;
    }

    /// <summary>
    /// Marks agents whose shift has ended. They finish current chats but get no new ones.
    /// </summary>
    public void UpdateAgentShiftStatus(List<Agent> agents, DateTime utcNow)
    {
        var currentShift = GetCurrentShift(utcNow);
        foreach (var agent in agents)
        {
            if (agent.IsOverflow)
            {
                // Overflow only active during office hours
                agent.IsShiftOver = !IsOfficeHours(utcNow);
            }
            else
            {
                agent.IsShiftOver = agent.Shift != currentShift;
            }
        }
    }
}