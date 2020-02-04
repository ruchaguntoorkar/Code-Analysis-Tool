# Code-Analysis-Tool
The Remote Code Analyzer will extract the lexical content from source files, analyzing its code syntax, perform the dependency analysis using the typetable obtained from type analysis and finding strong components. Client gives the input files by browsing the subdirectories.

The user or client browses the subdirectories and select the files. On clicking the button, request message is sent to the server through message passing queue. Server and client are connected through comm. whenever the sender sends the request it is posted to sender queue. It is then popped from queue and sent to the receiver. The message is added to receiver’s receive queue, which then picked by the receiver. The client can request for get files, get directories, dependency analysis and strong component. Whenever the client request for dependency analysis, type analysis is performed first on the semiExpression return by the lexer. While doing type analysis, semiExpressions are checked against the rules in the parser. Typetable holds the result of type analysis. The files are again passed for the dependency analysis. This time semiExpression are checked against the typetable. The result is stored in the dependency table. Graph is constructed using the dependency table and strong component analysis is done using Tarjan’s algorithm. 

This project will provide the following capabilities: 
 Client can browse through the subdirectories and files from server side
 Client can request for strong component and dependency analysis, server files and directories by passing messages 
 Server can get the client request and can send reply message containing the result of the request
 Server can perform dependency analysis, type analysis, parsing and finding strong components, lexical analysis, returning list of files   and sub-directories requested by the server
