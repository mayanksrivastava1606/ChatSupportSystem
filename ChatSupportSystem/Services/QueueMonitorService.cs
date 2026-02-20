using ChatSupportSystem.Models;

namespace ChatSupportSystem.Services;

/// <summary>
/// Background service that monitors chat sessions every second.
/// Marks sessions as inactive after 3 missed polls.
/// Also continuously tries to assign queued chats.
/// </summary>
public class QueueMonitorService : BackgroundService
{
    private readonly ChatCoordinator _coordinator;
    private readonly ChatQueue _chatQueue;
    private readonly ChatAssignmentService _assignmentService;
    private readonly ShiftManager _shiftManager;
    private readonly ILogger<QueueMonitorService> _logger;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);
    private const int MaxMissedPolls = 3;

    public QueueMonitorService(
        ChatCoordinator coordinator,
        ChatQueue chatQueue,
        ChatAssignmentService assignmentService,
        ShiftManager shiftManager,
        ILogger<QueueMonitorService> logger)
    {
        _coordinator = coordinator;
        _chatQueue = chatQueue;
        _assignmentService = assignmentService;
        _shiftManager = shiftManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("QueueMonitorService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                MonitorSessions();
                AssignPendingChats();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in QueueMonitorService.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private void MonitorSessions()
    {
        var now = DateTime.UtcNow;
        var sessions = _chatQueue.GetAllSessions();

        foreach (var session in sessions)
        {
            if (session.Status is ChatSessionStatus.Inactive or ChatSessionStatus.Refused)
                continue;

            // Check if the session has missed polls (1s interval expected)
            var elapsed = now - session.LastPollAt;
            if (elapsed.TotalSeconds >= 1)
            {
                session.MissedPolls++;
            }

            if (session.MissedPolls >= MaxMissedPolls)
            {
                session.Status = ChatSessionStatus.Inactive;
                _logger.LogInformation("Session {SessionId} marked inactive after {Missed} missed polls.",
                    session.Id, session.MissedPolls);

                // Free agent slot
                if (session.AssignedAgentId.HasValue)
                {
                    var agent = _coordinator.AllAgents
                        .FirstOrDefault(a => a.Id == session.AssignedAgentId.Value);
                    agent?.RemoveChat(session.Id);
                }
            }
        }
    }

    private void AssignPendingChats()
    {
        var utcNow = DateTime.UtcNow;
        _shiftManager.UpdateAgentShiftStatus(_coordinator.AllAgents, utcNow);
        _assignmentService.AssignAllPending(_chatQueue, _coordinator.AllAgents);
    }
}