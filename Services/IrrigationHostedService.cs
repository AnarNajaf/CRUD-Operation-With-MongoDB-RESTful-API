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
                await CheckAndRunAutoMode();
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

                // ── Day-of-week filter ────────────────────────────────────────
                if (schedule.AllowedDays?.Count > 0 &&
                    !schedule.AllowedDays.Contains((int)now.DayOfWeek))
                {
                    _logger.LogDebug("Motor {Code} skipped — {Day} not in allowed days.", motor.DeviceCode, now.DayOfWeek);
                    continue;
                }

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

        // ── Auto mode: moisture-based start/stop ──────────────────────────────

        private async Task CheckAndRunAutoMode()
        {
            using var scope = _serviceProvider.CreateScope();
            var deviceService   = scope.ServiceProvider.GetRequiredService<DeviceService>();
            var firebaseService = scope.ServiceProvider.GetRequiredService<FirebaseService>();
            var logService      = scope.ServiceProvider.GetRequiredService<LogService>();

            var autoMotors = await deviceService.GetMotorsByModeAsync("auto");
            var now = DateTime.UtcNow;

            foreach (var motor in autoMotors)
            {
                if (motor.LinkedSensorCodes == null || motor.LinkedSensorCodes.Count == 0)
                    continue;

                // Collect fresh moisture readings from linked sensors
                var readings = new List<double>();
                foreach (var code in motor.LinkedSensorCodes)
                {
                    var (moisture, updatedAt) = await firebaseService.GetSensorMoistureAsync(code);
                    if (moisture == null) continue;
                    // Skip stale data older than 30 minutes
                    if (updatedAt != null && (now - updatedAt.Value).TotalMinutes > 30) continue;
                    readings.Add(moisture.Value);
                }

                if (readings.Count == 0)
                {
                    _logger.LogDebug("Auto motor {Code} — all linked sensors stale or missing, skipping.", motor.DeviceCode);
                    continue;
                }

                var avgMoisture = readings.Average();
                _logger.LogDebug("Auto motor {Code} avg moisture: {Avg:F1}% (lower={Low}, upper={High})",
                    motor.DeviceCode, avgMoisture, motor.LowerThreshold, motor.UpperThreshold);

                if (!motor.IsActive && avgMoisture < motor.LowerThreshold)
                {
                    // Soil is dry — turn ON
                    var result = await deviceService.UpdateMotorStatusAsync(motor.Id, motor.FarmerId, true);
                    if (result.success)
                    {
                        await firebaseService.SetMotorActiveAsync(motor.DeviceCode, true);
                        await logService.LogAsync(motor.Id, motor.DeviceCode, "auto_started", true,
                            $"Avg moisture {avgMoisture:F1}% < threshold {motor.LowerThreshold}%");
                        _logger.LogInformation("Auto: motor {Code} started (moisture {Avg:F1}% < {Low}%)",
                            motor.DeviceCode, avgMoisture, motor.LowerThreshold);

                        // Safety: schedule auto-stop after AutoMaxRuntimeMinutes
                        if (motor.AutoMaxRuntimeMinutes > 0)
                        {
                            var motorId = motor.Id;
                            var farmerId = motor.FarmerId;
                            var deviceCode = motor.DeviceCode;
                            var maxMin = motor.AutoMaxRuntimeMinutes;
                            _ = Task.Run(async () =>
                            {
                                await Task.Delay(TimeSpan.FromMinutes(maxMin));
                                // Only stop if still in auto mode and still active
                                var current = await deviceService.GetMotorByIdAsync(motorId, farmerId);
                                if (current?.Mode == "auto" && current.IsActive)
                                {
                                    await deviceService.UpdateMotorStatusAsync(motorId, farmerId, false);
                                    await firebaseService.SetMotorActiveAsync(deviceCode, false);
                                    await logService.LogAsync(motorId, deviceCode, "auto_timeout", true,
                                        $"Auto max runtime {maxMin} min reached.");
                                }
                            });
                        }
                    }
                }
                else if (motor.IsActive && avgMoisture >= motor.UpperThreshold)
                {
                    // Soil is wet enough — turn OFF
                    var result = await deviceService.UpdateMotorStatusAsync(motor.Id, motor.FarmerId, false);
                    if (result.success)
                    {
                        await firebaseService.SetMotorActiveAsync(motor.DeviceCode, false);
                        await logService.LogAsync(motor.Id, motor.DeviceCode, "auto_stopped", true,
                            $"Avg moisture {avgMoisture:F1}% >= threshold {motor.UpperThreshold}%");
                        _logger.LogInformation("Auto: motor {Code} stopped (moisture {Avg:F1}% >= {High}%)",
                            motor.DeviceCode, avgMoisture, motor.UpperThreshold);
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

                int maxRuntime;
                if ((schedule?.MaxRuntimeMinutes ?? 0) > 0)
                {
                    // Explicit limit configured — applies to any mode
                    maxRuntime = schedule!.MaxRuntimeMinutes;
                }
                else if (motor.Mode != "manual")
                {
                    // Scheduled/auto motors get the default safety cap
                    maxRuntime = DefaultMaxRuntimeMinutes;
                }
                else
                {
                    // Manual mode with no explicit limit — farmer is in control, don't interfere
                    continue;
                }

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

            // Don't interfere if a manual override is already running
            if (motor.IsActive)
            {
                _logger.LogDebug("Motor {Code} already active — skipping scheduled trigger.", motor.DeviceCode);
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
