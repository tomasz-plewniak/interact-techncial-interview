# Production Engineering Technical Challenge

A two-stage technical assessment for a Production Engineering role.

---

## Step 1 — Home Assessment: Delimited File Merger

### Task

Given multiple delimiter-separated, UTF-8 encoded files that share a common `ID` column, read all files and produce a single merged output file where each row combines all properties for a given ID.

**Example input (two files):**

| ID | Property 1 |     | ID | Property 2 | Property 3 |
|----|------------|-----|----|------------|------------|
| A  | XXXX       |     | D  | 1111       | A1A1       |
| B  | YYYY       |     | B  | 2222       | B2B2       |
| C  | ZZZZ       |     | A  | 3333       | C3C3       |
| D  | AAAA       |     | C  | 4444       | D4D4       |

**Expected merged output:**

| ID | Property 1 | Property 2 | Property 3 |
|----|------------|------------|------------|
| A  | XXXX       | 3333       | C3C3       |
| B  | YYYY       | 2222       | B2B2       |
| C  | ZZZZ       | 4444       | D4D4       |
| D  | AAAA       | 1111       | A1A1       |

### Constraints

- No third-party CSV/delimited-file parsing frameworks
- No high-level data-joining frameworks (e.g. DataTable)
- All parsing and join logic written from scratch
- Solution must accept input file paths and an output path as CLI arguments
- Solution must measure and display total execution time

### Implementation

- **Language:** C# / .NET 10
- **Location:** [`1-Home-Assesment/`](./1-Home-Assesment/)
- Auto-detects delimiter by analysing the header line — supports any delimiter
- The `ID` column can appear at any position in a file
- Records missing from one file produce empty values for that file's columns in the final output

### Performance (live dataset — ~29 MB output)

| Run | dev (ms) | live (ms) |
|-----|----------|-----------|
| 1   | 28       | 2808      |
| 2   | 27       | 2720      |
| 3   | 27       | 2737      |
| 4   | 27       | 2724      |
| 5   | 28       | 2739      |
| **AVG** | **27.4** | **2745.6** |

Tested on: MacBook Pro 13-inch, 2 GHz Quad-Core Intel Core i5, 16 GB RAM.

### How to run

```bash
cd 1-Home-Assesment/src/FileMerger
dotnet build -c Release
dotnet run -- <file1> <file2> ... <fileN> <output_path>
```

---

## Step 2 — Live Coding: Berlin Clock Simulator

### Task

Implement the Berlin Clock (Mengenlehre-Uhr) — a clock that represents time using rows of illuminated lamps rather than digits.

### Clock structure

| Row | Lamps | Colour | Represents |
|-----|-------|--------|------------|
| Top | 1 | Yellow | Blinks ON for even seconds, OFF for odd |
| Row 1 | 4 | Red | Each lamp = 5 hours |
| Row 2 | 4 | Red | Each lamp = 1 hour |
| Row 3 | 11 | Yellow / Red | Each lamp = 5 minutes; positions 3, 6, 9 are red (quarter hours) |
| Row 4 | 4 | Yellow | Each lamp = 1 minute |

### Implementation

- **Language:** C# / .NET 6+
- **Location:** [`2-Live-coding/BerlinClock/`](./2-Live-coding/BerlinClock/)
- Console application — accepts time in `HH:MM:SS` format interactively
- Type `exit` to quit

### How to run

```bash
cd 2-Live-coding/BerlinClock
dotnet run
```

Enter a time when prompted, e.g. `13:17:01`.
