using iTarlaMapBackend.Services;

namespace iTarlaMapBackend.BackgroundServices
{
    public class IrrigationHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<IrrigationHostedService> _logger;

        // Default max runtime for motors with no schedule (manual mode safety net)
        private const int DefaultMaxRuntimeMinutes = 240;

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
                await RunWatchdog();
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        // ── Schedule runner ───────────────────────────────────────────────────

        private async Task CheckAndRunSchedules()
        {
            using var scope = _serviceProvider.CreateScope();
            var scheduleService = scope.ServiceProvider.GetRequiredService<ScheduleService>();
            var deviceService   = scope.ServiceProvider.GetRequiredService<DeviceService>();
            var firebaseService = scope.ServiceProvider.GetRequiredService<FirebaseService>();
            var logService      = scope.ServiceProvider.GetRequiredService<LogService>();

            var schedules = await scheduleService.GetAllEnabledAsync();
            var now = DateTime.UtcNow;

            foreach (var schedule in schedules)
            {
                var motor = await deviceService.GetMotorByIdAsync(schedule.MotorId, schedule.FarmerId);
                if (motor == null || motor.Mode != "scheduled") continue;

                // ── Forbidden hours check ─────────────────────────────────────
                if (IsInForbiddenWindow(schedule, now))
                {
                    _logger.LogDebug("Motor {Code} skipped — forbidden hour {Hour}.", motor.DeviceCode, now.Hour);
                    continue;
                }

                // ── Data freshness check ──────────────────────────────────────
                if (schedule.DataFreshnessMinutes > 0 && !string.IsNullOrEmpty(schedule.LinkedSensorCode))
                {
                    var lastUpdate = await firebaseService.GetSensorLastUpdateAsync(schedule.LinkedSensorCode);
                    if (lastUpdate == null || (now - lastUpdate.Value).TotalMinutes > schedule.DataFreshnessMinutes)
                    {
                        var reason = lastUpdate == null ? "no data" : $"data is {(int)(now - lastUpdate.Value).TotalMinutes} min old";
                        await logService.LogAsync(schedule.MotorId, motor.DeviceCode, "skipped_stale_sensor", false, reason);
                        _logger.LogWarning("Motor {Code} skipped — sensor data stale ({Reason}).", motor.DeviceCode, reason);
                        continue;
                    }
                }

                if (schedule.ScheduleType == "time")
                {
                    foreach (var window in schedule.TimeWindows)
                    {
                        if (!TimeSpan.TryParse(window.StartTime, out var windowTime)) continue;

                        var scheduledAt = now.Date + windowTime;
                        var minutesSince = (now - scheduledAt).TotalMinutes;

                        if (minutesSince < 0 || minutesSince >= 2) continue;

                        if (schedule.LastRunDates.TryGetValue(window.StartTime, out var lastRun) &&
                            lastRun.Date == now.Date)
                            continue;

                        await TriggerIrrigation(
                            schedule, window.DurationMinutes,
                            deviceService, scheduleService, firebaseService, logService,
                            windowKey: window.StartTime);
                    }
                }
                else
                {
                    var lastRan = schedule.LastRanAt ?? schedule.CreatedAt;
                    if ((now - lastRan).TotalHours >= schedule.IntervalHours)
                    {
                        await TriggerIrrigation(
                            schedule, schedule.DurationMinutes,
                            deviceService, scheduleService, firebaseService, logService);
                    }
                }
            }
        }

        // ── Watchdog: force-stop motors that have exceeded max runtime ─────────

        private async Task RunWatchdog()
        {
            using var scope = _serviceProvider.CreateScope();
            var deviceService   = scope.ServiceProvider.GetRequiredService<DeviceService>();
            var scheduleService = scope.ServiceProvider.GetRequiredService<ScheduleService>();
            var firebaseService = scope.ServiceProvider.GetRequiredService<FirebaseService>();
            var logService      = scope.ServiceProvider.GetRequiredService<LogService>();

            var activeMotors = await deviceService.GetAllActiveMotorsAsync();
            var now = DateTime.UtcNow;

            foreach (var motor in activeMotors)
            {
                if (motor.ActiveSince == null) continue;

                var minutesOn = (now - motor.ActiveSince.Value).TotalMinutes;

                var schedule = await scheduleService.GetByMotorIdDirectAsync(motor.Id);
                var maxRuntime = (schedule?.MaxRuntimeMinutes ?? 0) > 0
                    ? schedule!.MaxRuntimeMinutes
                    : DefaultMaxRuntimeMinutes;

                if (minutesOn < maxRuntime) continue;

                _logger.LogWarning(
                    "Watchdog: motor {Code} has been ON for {Minutes} min (limit {Max}). Force stopping.",
                    motor.DeviceCode, (int)minutesOn, maxRuntime);

                await deviceService.UpdateMotorStatusAsync(motor.Id, motor.FarmerId, false);
                await firebaseService.SetMotorActiveAsync(motor.DeviceCode, false);
                await logService.LogAsync(motor.Id, motor.DeviceCode, "safety_timeout", true,
                    $"Motor was ON for {(int)minutesOn} min, limit is {maxRuntime} min.");
            }
        }

        // ── Irrigation trigger ────────────────────────────────────────────────

        private async Task TriggerIrrigation(
            iTarlaMapBackend.Models.Schedule schedule,
            int requestedDuration,
            DeviceService deviceService,
            ScheduleService scheduleService,
            FirebaseService firebaseService,
            LogService logService,
            string? windowKey = null)
        {
            var motor = await deviceService.GetMotorByIdAsync(schedule.MotorId, schedule.FarmerId);
            if (motor == null)
            {
                _logger.LogWarning("Motor {MotorId} not found, skipping.", schedule.MotorId);
                return;
            }

            // Cap duration to MaxRuntimeMinutes if set
            var effectiveDuration = (schedule.MaxRuntimeMinutes > 0)
                ? Math.Min(requestedDuration, schedule.MaxRuntimeMinutes)
                : requestedDuration;

            var result = await deviceService.UpdateMotorStatusAsync(schedule.MotorId, schedule.FarmerId, true);
            if (!result.success)
            {
                await logService.LogAsync(schedule.MotorId, motor.DeviceCode, "started", false, result.message);
                _logger.LogWarning("Could not turn on motor {Code}: {Message}", motor.DeviceCode, result.message);
                return;
            }

            await firebaseService.SetMotorActiveAsync(motor.DeviceCode, true);

            if (windowKey != null)
                await scheduleService.UpdateTimeWindowLastRunAsync(schedule.Id, windowKey);
            else
                await scheduleService.UpdateLastRanAtAsync(schedule.Id);

            await logService.LogAsync(schedule.MotorId, motor.DeviceCode, "started", true);
            _logger.LogInformation("Irrigation started for {Code}, effective duration {Duration}min (requested {Requested}min)",
                motor.DeviceCode, effectiveDuration, requestedDuration);

            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(effectiveDuration));

                var offResult = await deviceService.UpdateMotorStatusAsync(
                    schedule.MotorId, schedule.FarmerId, false);
                await firebaseService.SetMotorActiveAsync(motor.DeviceCode, false);
                await logService.LogAsync(schedule.MotorId, motor.DeviceCode, "completed", offResult.success);

                _logger.LogInformation("Irrigation completed for {Code}. Success: {Success}",
                    motor.DeviceCode, offResult.success);
            });
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static bool IsInForbiddenWindow(iTarlaMapBackend.Models.Schedule schedule, DateTime utcNow)
        {
            if (schedule.ForbiddenFromHour == null || schedule.ForbiddenToHour == null) return false;

            var hour = utcNow.Hour;
            var from = schedule.ForbiddenFromHour.Value;
            var to   = schedule.ForbiddenToHour.Value;

            // Handles both normal (6→14) and wrap-around (22→6) ranges
            return from < to
                ? hour >= from && hour < to
                : hour >= from || hour < to;
        }
    }
}
