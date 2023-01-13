# TCP C# Client
This is a C# Windows console application. The client called location and it uses TCP sockets to communicate with a TCP server.
The client and the server used for a simple student locating facility. The client simply use the Microsoft Command Prompt interface to run. 
The client and server communicate using simplified forms of the internet whois (RFC3912), HTTP/0.9 (w3.org), HTTP/1.0 (RFC1945) and HTTP/1.1 (RFC2616) protocols.

# BASIC CLIENT INTERFACE SPECIFICATION 
The client (which must be called location) can have two arguments, a user name and a string 
giving location information. In the examples below, the text G:\500081\> represents the 
command prompt. 
For example:
```
G:\500081\> location cssbct 
cssbct is in RB-336 
```
In this example, location is the name of the executable of the client program, which is given 
one argument (cssbct) and the client shows the response "cssbct is in RB-336". This string is 
composed by the client from the argument supplied and the data returned by the location 
server. The client must output only this for a successful location lookup. 

```
G:\500081\> location cssbct "in RB-310 for a meeting" 
cssbct location changed to be in RB-310 for a meeting 
```
In this example location is given two arguments, the first cssbct is the user name being 
updated, and the second is the string "in RB-310 for a meeting". The quote characters (") are 
not passed to the client as part of the second argument, they are used to indicate to the windows 
command interpreter that the sequence of words separated by spaces are part of a single 
argument and are not five consecutive arguments to the client. 
The response output by the client shows that the server has acknowledged the update was 
successful and is composed from the arguments to the client. 
```
G:\500081\> location cssbct 
cssbct is in RB-310 for a meeting 
```
This example shows an enquiry to the server after making the previous update, which 
illustrates the new location is returned for further queries. It also allows you to determine the 
specified syntax of replies. 

In summary these examples show first the call of the client indicating the current location of 
the user cssbct. The second was used to change the location and the third showed the 
changed response. 
