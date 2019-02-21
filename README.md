# Tailf

Tailf is a C# implementation of the tail -f command available on unix/linux systems. Differently form other ports it does not lock the file in any way so it works even if other rename the file: this is expecially designed to works well with log4net rolling file appender.

You will probably use tailf to monitor log files in order to see new messages as soon they are enqueued. Additionally a filter could be added to show only the lines containing a pattern matching a given regular expression. The usual log4net rolling file appender is not prevented to created regular backup when tailf monitoring is active.

The project in this fork compiles Tailf into a .net library, rather than console application. 
