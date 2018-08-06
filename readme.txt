The parser follows some pretty simplistic rules and may throw away some lines that are important to you.
Use at your own risk. Understand what it is doing in Form1.button1.Click().

You can click the DateTime and Time columns to sort ascending. Double-click to sort descending.
When I created this I was looking for collections with a lot of "COLLSCAN"s so the bottom grid is sorted by ColScans.
The chart is showing the selected row in the lower grid. It is capped at 90 seconds.

Drop Time Outs only works as the file is being loaded. 
It drops lines with a Time greater than 1271310000, thinking it is probably just a fail.
I did this so that the average time in the lower grid would not bee so distorted.
Hopefully you'll never need to use this.

Column widths will auto-size on double-click. (Useful for the Line & DateLine column.)

