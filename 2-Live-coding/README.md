# Berlin Clock (Mengenlehre-Uhr) Simulator

A .NET console application that simulates the Berlin Clock, also known as the Mengenlehre-Uhr (Set Theory Clock).

## What is the Berlin Clock?

The Berlin Clock displays time using colored lights instead of digits. It consists of 5 rows:

### Row Structure

1. **Top Lamp (Seconds Indicator)**
   - Yellow lamp that blinks
   - **ON (Y)** = even seconds (0, 2, 4, ..., 58)
   - **OFF (O)** = odd seconds (1, 3, 5, ..., 59)

2. **First Row (5-Hour Blocks)**
   - 4 red lamps
   - Each lamp = 5 hours
   - Lit lamps = hours ÷ 5

3. **Second Row (1-Hour Blocks)**
   - 4 red lamps
   - Each lamp = 1 hour
   - Lit lamps = hours % 5

4. **Third Row (5-Minute Blocks)**
   - 11 lamps (yellow, with every 3rd being red for quarter hours)
   - Each lamp = 5 minutes
   - Lit lamps = minutes ÷ 5
   - Positions 3, 6, 9 are **red (R)** for quarter hours
   - Other positions are **yellow (Y)**

5. **Fourth Row (1-Minute Blocks)**
   - 4 yellow lamps
   - Each lamp = 1 minute
   - Lit lamps = minutes % 5

## Legend

- **●** = Lamp ON (colored)
- **○** = Lamp OFF
- **R** = Red
- **Y** = Yellow
- **O** = Off

## How to Run

```bash
dotnet run
```

Then enter time in format `HH:MM:SS` (e.g., `13:17:01`)

Type `exit` to quit.

## Examples

### Example 1: 13:17:01
```
Seconds: ○ (O - OFF)           # Odd second = OFF
5-Hours: ● ● ○ ○ (RROO)        # 13÷5 = 2 lamps (10 hours)
1-Hours: ● ● ● ○ (RRRO)        # 13%5 = 3 lamps (3 hours)
5-Mins:  ● ● ● ○ ... (YYROO...)# 17÷5 = 3 lamps (15 minutes)
1-Mins:  ● ● ○ ○ (YYOO)       # 17%5 = 2 lamps (2 minutes)
```
Total: 10+3 = 13 hours, 15+2 = 17 minutes, 1 second

### Example 2: 23:59:58
```
Seconds: ● (Y - ON)            # Even second = ON
5-Hours: ● ● ● ● (RRRR)        # 23÷5 = 4 lamps (20 hours)
1-Hours: ● ● ● ○ (RRRO)        # 23%5 = 3 lamps (3 hours)
5-Mins:  ● ● ● ... (YYRYYR...) # 59÷5 = 11 lamps (55 minutes)
1-Mins:  ● ● ● ● (YYYY)        # 59%5 = 4 lamps (4 minutes)
```
Total: 20+3 = 23 hours, 55+4 = 59 minutes, 58 seconds

### Example 3: 00:00:00 (Midnight)
```
Seconds: ● (Y - ON)            # Even second = ON
5-Hours: ○ ○ ○ ○ (OOOO)       # All OFF
1-Hours: ○ ○ ○ ○ (OOOO)       # All OFF
5-Mins:  ○ ○ ○ ... (OOOOO...)  # All OFF
1-Mins:  ○ ○ ○ ○ (OOOO)       # All OFF
```

## Project Structure

```
BerlinClock/
├── BerlinClock.csproj
└── Program.cs
```

## Requirements

- .NET 6.0 or higher
