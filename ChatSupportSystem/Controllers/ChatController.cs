using ChatSupportSystem.Models;
using ChatSupportSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatSupportSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ChatCoordinator _coordinator;

    public ChatController(ChatCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    /// <summary>
    /// Creates a new chat session and queues it.
    /// Returns "OK" if accepted (client should start polling every 1s).
    /// Returns "Refused" if the queue is full.
    /// </summary>
    [HttpPost]
    public ActionResult<CreateChatResponse> CreateSession()
    {
        var response = _coordinator.CreateChatSession();

        if (response.Status == ChatSessionStatus.Refused)
            return StatusCode(503, response);

        return Ok(response);
    }

    /// <summary>
    /// Client polls every 1 second to keep the session alive.
    /// Missing 3 consecutive polls marks the session inactive.
    /// </summary>
    [HttpPost("{sessionId}/poll")]
    public ActionResult<PollResponse> Poll(Guid sessionId)
    {
        var response = _coordinator.Poll(sessionId);

        if (response.Status == ChatSessionStatus.Inactive)
            return NotFound(response);

        return Ok(response);
    }

    /// <summary>
    /// Returns the current system status for monitoring/debugging.
    /// </summary>
    [HttpGet("status")]
    public ActionResult GetStatus()
    {
        var utcNow = DateTime.UtcNow;
        var agents = _coordinator.AllAgents;

        return Ok(new
        {
            CurrentTime = utcNow,
            TeamCapacity = _coordinator.GetCurrentTeamCapacity(utcNow),
            OverflowCapacity = _coordinator.GetOverflowCapacity(utcNow),
            MaxQueueLength = _coordinator.GetMaxQueueLength(utcNow),
            ActiveAgents = agents
                .Where(a => !a.IsShiftOver)
                .Select(a => new
                {
                    a.Name,
                    a.TeamName,
                    Seniority = a.Seniority.ToString(),
                    a.MaxConcurrency,
                    a.AvailableSlots,
                    ActiveChats = a.ActiveSessions.Count,
                    a.IsOverflow
                })
        });
    }
}