[Engine Simulator Vehicle Systems.md](https://github.com/user-attachments/files/33607637/Engine.Simulator.Vehicle.Systems.md)
# Engine Simulator: Vehicle Systems

Oct 17, 2026 · @Jeremy

An engine needs supporting systems to start, stay running and survive. Each system below is described by its job, the state it holds and what it talks to, so each maps cleanly to a class.

## The container: Vehicle

The Vehicle holds everything and runs the update loop. Each tick it updates every system in order and passes values between them.

- The engine shouldn't reach into the battery directly. The Vehicle connects them.
- `AmbientConditions` lives here too, since every system shares the same outside air.

## The engine: RPM model

RPM comes from net torque, and every system below acts on it: ignition zeroes torque, fuel drains with RPM, and the ECU holds the idle floor.

- **Net torque** = engine torque − friction. Engine torque is 0 while spark is cut.
- **RPM change** = net torque ÷ inertia, times `dt`.
- **Inertia:** a tunable variable. Higher means RPM responds more slowly.
- **Friction:** constant for the MVP. Friction that rises with RPM comes later.
- **Lives in:** `Engine.Update`, at minimum a method on the Engine class.

## 1. Electrical system

For the MVP, the engine starts if a battery is connected. Nothing else in the electrical system matters yet.

- **Battery:** either connected or not. Charge doesn't matter.
- **Starter:** when activated, sets RPM straight to idle. Real cranking comes later.
- **Battery check:** the ECU checks it before starting, so the Engine never needs to know batteries exist.
- **No battery:** the start fails with a "battery not connected" message.

## 2. Fuel system

For the MVP, the fuel tank drains by the fuel the engine actually burns, and the sim ends when it runs dry.

- **Tank:** capacity in liters (`FuelTankSizeL`) plus a separate current level. Starts full.
- **Drain:** fuel burned per cycle (from Air intake) × RPM / 120 × `dt`, converted from kilograms to liters. A 4-stroke engine completes one cycle every 2 revolutions, hence RPM / 120.
- **Fuel properties:** density (about 0.745 kg/L for gasoline), energy content (44,000,000 J/kg) and air-fuel ratio (14.7) are variables, so other fuel types can be tried later.
- **Empty tank:** the ECU stalls the engine immediately, and the sim ends.
- **Fuel pump:** an empty placeholder for now. Flow rate and load come later.

## 3. Ignition system

For the MVP, the ignition class holds whether spark is on or off. The ECU decides when to cut it.

- **Spark state:** on or off, set by the ECU's rev limiter.
- **Effect on the engine:** while spark is off, engine torque is 0, so friction slows the engine down.
- **Later:** spark timing that affects torque.

## 4. Air intake

For the MVP, engine torque comes from real air math: the air the engine takes in sets how much fuel burns and how much work it makes.

1. **Air mass per cycle** = VE × air density × displacement × throttle
2. **Fuel burned** = air mass ÷ 14.7
3. **Energy released** = fuel mass × 44,000,000 J/kg
4. **Useful work** = energy × thermal efficiency
5. **Torque** = work ÷ 4π

- **Air density:** a set variable in `AmbientConditions`. Calculating it from pressure and temperature comes later.
- **VE:** constant for the MVP. A two-sided curve already exists and can be applied to VE later to shape the torque curve.
- **Throttle:** lives on the Engine and scales air mass directly. It reports throttle position to the ECU, like a real throttle position sensor.
- **Minimum throttle opening:** the throttle never fully closes, so idle still draws air and burns fuel, like a real idle air bypass.
- **Thermal efficiency:** a fixed constant, about 0.30–0.35. Not meant to be tuned.
- **Air filter:** assumed present, not modeled.
- **Later:** manifold pressure or a MAF sensor in place of the simple throttle scaling.

## 5. ECU (engine control unit)

The ECU holds every control decision: whether the engine may start, when it stalls, the rev limiter and the idle floor. The Vehicle passes it what it needs each tick.

- **Start checks:** refuses to start without a connected battery, with a "battery not connected" message.
- **Fuel stall:** stalls the engine immediately when the tank hits zero, which ends the sim.
- **Rev limiter:** cuts spark at the calculated max RPM and restores it once RPM drops below max by a tunable gap, about 100–200 RPM. The gap keeps spark from flickering every tick.
- **Idle floor:** keeps RPM from dropping below the random idle value while the engine runs.
- **Inputs:** the Vehicle passes RPM in each tick. The ECU holds no Engine reference.
- **Later:** hold idle by adjusting throttle instead of a hard floor.

## 6. Assumed, not modeled

These systems are treated as always working in the MVP.

- **Cooling:** the engine never overheats.
- **Lubrication:** oil is always present.
- **Exhaust:** no effect on power.
- **Air filter:** present, no restriction.

## 7. Starting and the tick loop

One key starts the engine; after that, every tick reads the current values, decides what's commanded, then runs the math to move toward it.

**Starting**

1. The user presses the engine start key. Pressing it while the engine runs does nothing. W and S still raise and lower the throttle.
2. The ECU checks for a connected battery and fuel in the tank.
3. If a check fails, the start fails with a message, such as "battery not connected."
4. If both pass, the starter sets RPM to the random idle value and the engine is running.

**Each tick while running**

1. **Read:** current RPM, fuel level and throttle input.
2. **Decide:** the throttle updates from input. The ECU stalls the engine if the tank is empty and sets spark on or off for the rev limiter.
3. **Act:** the engine calculates air, fuel burned and torque, then updates RPM from net torque. The tank drains by the fuel burned.
4. **Idle floor:** the ECU clamps RPM to idle if the math dropped it below.
5. **Display** the values.

## 8. Build order

The build starts with the RPM rework, then follows a real start sequence. The ECU grows one check at a time alongside the part it checks.

1. **RPM rework:** net torque, inertia and constant friction in `Engine.Update`, using the current torque model for now. Add the ECU's idle floor here.
2. **Battery:** connected or not, plus the ECU's battery check.
3. **Starter and start key:** the start command, and the starter setting RPM to idle.
4. **Fuel tank:** capacity, level, the ECU's fuel check and the empty-tank stall. Drain is wired up in step 5.
5. **Air:** `AmbientConditions`, fuel properties, the minimum throttle opening, and torque from the air math. Swap it in for the old torque model and connect fuel drain.
6. **Ignition:** spark state and the ECU's rev limiter.

Already built: engine specs, max RPM, throttle and random idle.

## 9. Constants

The sim builds any engine from bore, stroke and cylinder count, so no constant is tied to one engine. Values that depend on engine size are stored per liter or as a pressure and scale with displacement automatically.

| Constant | Default | Units | Lives in | How it's used |
| --- | --- | --- | --- | --- |
| Bore, stroke, cylinder count | User input | mm, count | Engine | Defines the engine; displacement comes from these |
| Piston speed limit | 20 | m/s | Engine | Max RPM = 30 × limit / stroke |
| Base idle | 700 | RPM | Engine | Center of the random idle |
| Idle variation | ±10% | % of base idle | Engine | Range of the random idle |
| Rev limiter restore gap | 100–200 | RPM | ECU | Spark returns this far below max RPM |
| Minimum throttle opening | 0.03–0.05 | fraction | Engine | Idle air bypass |
| Volumetric efficiency (VE) | 0.9 | fraction | Engine | Air mass per cycle |
| Thermal efficiency | 0.30–0.35 | fraction | Engine | Fixed; share of fuel energy that becomes work |
| Inertia per liter | 0.05 | kg·m² per L | Engine | Inertia = value × displacement in liters |
| Friction pressure (FMEP) | 1.2 | bar | Engine | Friction torque = FMEP × displacement ÷ 4π |
| Air density | 1.18 | kg/m³ | AmbientConditions | Air mass per cycle |
| Fuel density | 0.745 | kg/L | Fuel properties | Kilograms to liters for drain |
| Fuel energy content | 44,000,000 | J/kg | Fuel properties | Energy released per kg burned |
| Air-fuel ratio | 14.7 | ratio | Fuel properties | Fuel mass from air mass |
| Fuel tank capacity | User input | L | Fuel tank | Starting fuel level |
