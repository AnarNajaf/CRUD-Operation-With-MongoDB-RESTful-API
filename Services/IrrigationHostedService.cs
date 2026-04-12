using iTarlaMapBackend.Services;

namespace iTarlaMapBackend.BackgroundServices
{
    public class IrrigationHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IrrigationHostedService> _logger;

    public IrrigationHostedService(
        IServiceProvider serviceProvider,
        ILogger<IrrigationHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Irrigation scheduler started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckAndRunSchedules();
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task CheckAndRunSchedules()
    {
        using var scope = _serviceProvider.CreateScope();
        var scheduleService = scope.ServiceProvider.GetRequiredService<ScheduleService>();
        var deviceService = scope.ServiceProvider.GetRequiredService<DeviceService>();
        var firebaseService = scope.ServiceProvider.GetRequiredService<FirebaseService>();

        var schedules = await scheduleService.GetAllEnabledAsync();
        var now = DateTime.UtcNow;

        foreach (var schedule in schedules)
        {
            if (schedule.LastRanAt == null ||
                (now - schedule.LastRanAt.Value).TotalHours >= schedule.IntervalHours)
            {
                await TriggerIrrigation(schedule, deviceService, scheduleService, firebaseService);
            }
        }
    }
    

    private async Task TriggerIrrigation(
        iTarlaMapBackend.Models.Schedule schedule,
        DeviceService deviceService,
        ScheduleService scheduleService,
        FirebaseService firebaseService)
    {
        // Get motor device code — needed for Firebase
        var motor = await deviceService.GetMotorByIdAsync(schedule.MotorId, schedule.FarmerId);

        if (motor == null)
        {
            _logger.LogWarning("Motor {MotorId} not found, skipping.", schedule.MotorId);
            return;
        }

        _logger.LogInformation("Irrigating motor {Code} for {Duration} min",
            motor.DeviceCode, schedule.DurationMinutes);

        // Turn ON in both MongoDB and Firebase
        await deviceService.UpdateMotorStatusAsync(schedule.MotorId, schedule.FarmerId, true);
        await firebaseService.SetMotorActiveAsync(motor.DeviceCode, true);
        await scheduleService.UpdateLastRanAtAsync(schedule.Id);

        // Wait then turn OFF in both
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromMinutes(schedule.DurationMinutes));

            await deviceService.UpdateMotorStatusAsync(schedule.MotorId, schedule.FarmerId, false);
            await firebaseService.SetMotorActiveAsync(motor.DeviceCode, false);

            _logger.LogInformation("Motor {Code} irrigation complete.", motor.DeviceCode);
        });
    }
}
}