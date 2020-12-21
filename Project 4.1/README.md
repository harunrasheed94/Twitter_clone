## COP5615 DISTRIBUTED OPERATING SYSTEMS

## PROJECT 4, PART I 
Twitter Clone and a client tester/simulator.

## Group Members
Mohammed Haroon Rasheed Kalilur Rahman - 6751 2967 \
Ariz Ahmad - 7111 2167

## Problem Description
Build an engine that (in part II) will be paired up with WebSockets to provide full functionality. 

## Instructions for Execution
* Run the Server.fsx file first using the command dotnet fsi --langversion:preview Server.fsx
* Run the Client.fsx file with the number of users at input dotnet fsi --langversion:preview Client.fsx <no. of users>

## Project Folder files
* Server.fsx
* Client.fsx
* types.fsx contains all the record objects

## Output
* Total simulation time for the entire process is done for three tweets per user.
* For 2000 users, with three to five tweets per user was the best our system performed, in terms of running time.
* Keeping number of tweets per client to 3-5, we could increase the number of clients to without much decrease in performance.
* When we tried to increase the number of tweets per client, our performance decreased.
* The  largest  number  of  clients  supported  for  our  system  was  3000,  withexecution time 319780 ms, and 19780 total requests.
