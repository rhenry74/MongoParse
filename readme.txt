The parser follows some pretty simplistic rules and may throw away some lines that are important to you.
Use at your own risk. Understand what it is doing in Form1.button1.Click().

You can click the DateTime and Time columns to sort ascending. Double-click to sort descending.
When I created this I was looking for collections with a lot of "COLLSCAN"s so the bottom grid is sorted by ColScans.
The chart is showing the selected row in the lower grid. It is capped at 90 seconds.

Drop Time Outs only works as the file is being loaded. 
It drops lines with a Time greater than 1271310000, thinking it is probably just a fail.
I did this so that the average time in the lower grid would not be so distorted.
Hopefully you'll never need to use this.

Column widths will auto-size on double-click. (Useful for the Line & DateTime column.)

Search will filter down lines based on entry, on 'Go' and restore all lines on 'No'.

Show trash will merge in lines that are excluded because they are not 'performance' related.

Filter List to Collection will filter the list to the collection selected in the bottom grid.

V2 / V3 selects a different parser, roughly related to the Mongo sevr version. The V2 parser is more vetted. Show Trash will not work with a V3 log file.

Warning: You can easily get the check boxes in a state that does not match the data in the list by mixing operations.


