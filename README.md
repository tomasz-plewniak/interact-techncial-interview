# FILE MERGER - TECHNICAL CHALLENGE

# Requirements
- .NET 10 SDK (net10.0)
- C#

# Building

Navigate to the FileMerger directory and run:

```bash
  dotnet build -c Release
```    

# Running the application
## Important!
Use the full path for files.

Usage: FileMerger <input_file1> <input_file2> ... <input_fileN> <output_path>

Example:

dev set
```bash
dotnet run -- /Users/tomaszplewniak/Projects/Other/Technical-Interviews/Interact/interact-challenge/data/dev/ _file_1.csv /Users/tomaszplewniak/Projects/Other/Technical-Interviews/Interact/interact-challenge/data/dev/small_file_2.csv /Users/tomaszplewniak/Projects/Other/Technical-Interviews/Interact/interact-challenge/data/dev/small_file_3.csv /Users/tomaszplewniak/Projects/Other/Technical-Interviews/Interact/interact-challenge/data/dev/small_file_4.csv /Users/tomaszplewniak/Projects/Other/Technical-Interviews/Interact/interact-challenge/data/dev/small_file_5.csv /Users/tomaszplewniak/Projects/Other/Technical-Interviews/Interact/interact-challenge/data/output/merged.csv
```

# Results:

| Run               | dev (ms) | live (ms) |
|-------------------|----------|-----------|
| 1                 | 28       | 2808      |
| 2                 | 27       | 2720      |
| 3                 | 27       | 2737      |
| 4                 | 27       | 2724      |
| 5                 | 28       | 2739      |
| AVG (ms)          | 27.4     | 2745.6    |
| File size (bytes) | 11628    | 28903547  |

# Tested on:
- MacBook Pro (13-inch)
- 2 GHz Quad-Core Intel Core i5
- 16 GB 3733 MHz LPDDR4X 

# Noteworthy observations
- Delimiter detection
The specification mentioned "delimiter separated" without specifying which delimiter. The parser auto-detects by
analyzing the header line.
- File 3 anomaly
In both dev and live datasets, file 3 has one fewer record:
  - Dev: 99 records (vs 100 in others)
  - Live: 249,999 records (vs 250,000 in others)
Handled correctly by filling missing columns with empty values.

- ID column flexibility
The ID column can be at any position, not just first.
sThe parser dynamically locates it by header name.

# Time of task completion
~ 4 h