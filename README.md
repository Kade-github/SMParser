# C# Parser for SM
Supports:
- Basic Metadata
- Notes (Fakes and mines too!)
- SM4 BPMS (no sm5 time shiz)

basically no SM5 shit, because im LAZY!

DO IT YOURSELF!!!

# Example

```c#
var smfile = new SMFile("path-to-your.sm");

Console.WriteLine(smFile); // outputs the title, and artist. Along with the difficulty count.

var difficulties = smfile.difficulties; // SMDifficulty
var timingPoints = smfile.timingPoints; // SMTimingPoint
```