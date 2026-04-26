# iTarla Map Backend

ASP.NET Core 9.0 REST API — the main backend for the iTarla smart irrigation system.
Handles farms, sensors, motors, schedules, irrigation logs, and runs the background automation engine.

---

## Architecture Overview

```
[Browser / Map Frontend]
        │  JWT Bearer token on every request
        ▼
[JwtIdentity Backend]  ─────────────────────────────────────────
        │  Issues JWT tokens (login / register)                  │
        ▼                                                         │
[iTarlaMapBackend]  ◄──────────────────────────────────────────┘
        │  Reads/writes farm, sensor, motor, schedule, log data
        │  Runs background irrigation scheduler every 60 seconds
        ▼
[MongoDB]            ←  persistent data (farms, motors, sensors, schedules, logs)
[Firebase Firestore] ←  real-time motor status & live sensor readings
```

> **Both backends must be running at the same time.**
> JwtIdentity handles auth only. iTarlaMapBackend handles everything else.

---

## Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 9.0+ | `dotnet --version` to check |
| MongoDB | 7.x | Must be running on `localhost:27017` |
| Firebase project | — | Project ID: `emocc-esp32` |

---

## Secret Files — NOT in Git (share privately with Anar)

Two files are intentionally excluded from git because they contain private credentials.
**You must create both before the project will run.**

### 1. `appsettings.Secrets.json`

Create this file in the project root (same folder as `appsettings.json`).
See `appsettings.Secrets.example.json` for the format:

```json
{
  "Jwt": {
    "Key": "YOUR_JWT_SECRET_KEY_HERE"
  }
}
```

> The `Key` value **must exactly match** the key used in the JwtIdentity backend.
> Get the actual value from Anar — do not generate a new one or all tokens will fail to validate.

---

### 2. `firebase-adminsdk.json`

Create this file in the project root (same folder as `appsettings.json`).

This is the Firebase service account private key — it lets the backend write motor states to Firestore
and read real-time sensor data for the irrigation automation.

**How to get it:**
1. Go to [Firebase Console](https://console.firebase.google.com) → Project `emocc-esp32`
2. Click the gear icon → **Project Settings** → **Service Accounts** tab
3. Click **"Generate new private key"** → download the JSON file
4. Rename it to `firebase-adminsdk.json` and place it in the project root

> Never commit this file to git. It contains a private RSA key.

---

## How to Run

```bash
cd CRUD-Operation-With-MongoDB-Main/iTarlaMapBackend
dotnet restore
dotnet run
```

- API runs at: `http://localhost:5212`
- Swagger UI: `http://localhost:5212/swagger` (development only)

---

## Project Structure

```
Controllers/
  FarmController.cs          CRUD for farm polygons
  SensorController.cs        CRUD + status toggle for sensors
  MotorController.cs         CRUD + on/off toggle + mode + auto-config
  ScheduleController.cs      Create/edit/delete irrigation schedules
  LogController.cs           Read & clear irrigation event logs
  FarmerController.cs        Farmer profile (auto-created from JWT claims)

Services/
  DeviceService.cs           Motor/sensor MongoDB operations
  ScheduleService.cs         Schedule MongoDB operations
  FarmService.cs             Farm MongoDB operations
  FarmerService.cs           Farmer profile management
  FirebaseService.cs         Read/write Firestore (motor status, sensor readings)
  LogService.cs              Irrigation event log
  IrrigationHostedService.cs Background engine (runs every 60 sec):
                               1. CheckAndRunSchedules  — time/interval-based irrigation
                               2. CheckAndRunAutoMode   — moisture-based auto irrigation
                               3. RunWatchdog           — force-stops overrunning motors

Models/            Motor, Sensor, Farm, Schedule, IrrigationLog, Farmer, ...
DTOs/              Request/response shapes for all endpoints
```

---

## API Reference

All endpoints require `Authorization: Bearer <token>` header.

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/motor` | List all motors for logged-in farmer |
| POST | `/api/motor` | Add a new motor |
| PUT | `/api/motor/{id}` | Update motor details |
| PATCH | `/api/motor/{id}/status` | Turn motor on/off |
| PATCH | `/api/motor/{id}/mode` | Set mode: manual / scheduled / auto |
| PATCH | `/api/motor/{id}/auto-config` | Save auto mode thresholds & linked sensors |
| DELETE | `/api/motor/{id}` | Remove motor |
| GET | `/api/sensor` | List all sensors |
| POST | `/api/sensor` | Add sensor |
| PATCH | `/api/sensor/{id}/status` | Enable/disable sensor |
| GET | `/api/schedule/motor/{motorId}` | Get schedule for a motor |
| POST | `/api/schedule` | Create or update schedule |
| PATCH | `/api/schedule/{id}/toggle` | Enable/disable schedule |
| DELETE | `/api/schedule/{id}` | Delete schedule |
| GET | `/api/log?skip=0&limit=50` | Paginated irrigation logs |
| DELETE | `/api/log/{id}` | Delete one log entry |
| DELETE | `/api/log` | Clear all logs |
| GET | `/api/farm/my` | Get farmer's farms |
| POST | `/api/farm` | Create farm with polygon |

---

## Background Engine — IrrigationHostedService

Runs every **60 seconds** automatically after startup. Three independent checks per tick:

### 1. Schedule Runner
- Finds all enabled schedules across all farmers
- **Interval mode**: triggers if `now - lastRanAt >= intervalHours`
- **Time-window mode**: triggers if the current minute falls within 2 min of a window start time (once per day per window)
- Respects: allowed days of week, forbidden hours, sensor data freshness check
- Turns motor ON → waits `durationMinutes` → turns motor OFF → logs event

### 2. Auto Mode
- Reads all motors in `auto` mode
- Fetches live soil moisture from Firebase for each linked sensor
- Avg moisture < `lowerThreshold` → turns motor ON
- Avg moisture >= `upperThreshold` → turns motor OFF
- `autoMaxRuntimeMinutes` triggers an automatic stop via a background Task

### 3. Watchdog
- Scans all currently active (ON) motors
- If a motor has been running longer than its `maxRuntimeMinutes` → force-stops it
- Logs a `safety_timeout` event

Each of the three checks is wrapped in its own try/catch so one crash does not break the others.

---

## MongoDB Collections

| Collection | Description |
|------------|-------------|
| `Farms` | Farm polygons with coordinates |
| `Farmers` | Farmer profiles (auto-created from JWT `sub` claim) |
| `Motors` | Motor devices — mode, thresholds, linked sensors, status |
| `Sensors` | Sensor devices |
| `Schedules` | Irrigation schedules per motor |
| `IrrigationLogs` | Event log: started, completed, auto_started, auto_stopped, safety_timeout, skipped_stale_sensor |

---

## Firebase Firestore Structure

```
Firestore/
  Motors/{deviceCode}
    isActive: bool
    checking: bool
    activeTimeHours: number
    updatedAt: timestamp

  Sensors/{deviceCode}
    soilMoisture: number
    temperature: number
    pH: number
    conductivity: number
    batteryLevel: number
    isOnline: bool
    lastUpdated: timestamp

  SystemStatus/global
    rainMode: bool
    tankLow: bool
    irrigationBlocked: bool
```

---

## Deployment Checklist

- [ ] .NET 9 SDK installed on server
- [ ] MongoDB running and accessible (update connection string in `appsettings.json` for production)
- [ ] `appsettings.Secrets.json` created with correct JWT key
- [ ] `firebase-adminsdk.json` placed in working directory (or set `GOOGLE_APPLICATION_CREDENTIALS` env variable to its path)
- [ ] CORS policy updated in `Program.cs` — currently `AllowAll`, tighten to your frontend domain for production
- [ ] JWT `Issuer` and `Audience` in `appsettings.json` must match the JwtIdentity backend config exactly
- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`

---

## Related Repositories

| Repo | Purpose |
|------|---------|
| [Map Frontend](https://github.com/AnarNajaf/Map) | Leaflet.js map, Firebase real-time UI |
| [JwtIdentity](https://github.com/abdullayevemil/JwtIdentity) | Auth backend — login, register, JWT issuance |
