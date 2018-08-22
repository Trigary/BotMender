# Packet documentation

A place where all of the packets are explained.

# UDP

 - All UDP packet's identifiers follow the following format: "Sender_Name"
 - The "Sender" can be "Client" or "Server" depending on which party is allowed to send the packet.
 - The "Name" is the name of the packet, it must make the packet identifier unique.

**Client_InputUpdate: the client informs the server of its current input.**  
8 bits: rightward | leftward | upward | downward | forward | backward | ignored | ignored  
Each bit represents whether the specified axis is pressed or not.  

**Server_StateUpdate: the server informs the client of X many bots' states.**  
X * 54 bytes: Byte-BotId, Byte-Input, Vector3-Position, Quaternion-Rotation, Vector3-Velocity, Vector3-AngularVelocity  
The input byte's format is specified in the "Client_InputUpdate" packet.  

# TCP

 - All TCP packets' identifiers follow the following format: "Sender_Category_[Subcategories]_Name"
 - The "Sender" can be "Client", "Server" or "Both" depending on which parties are allowed to send the packet.
 - The "Category" and the optional "Subcategories" are one/multiple of the categories present in this documentation.
 - The "Name" is the name of the packet, it must make the packet identifier unique.

## Initialization

**Server_State_Joined**  
X bytes: the IDs of the X players who joined (now or previously).

**Server_State_Left**  
1 byte: the ID of the player who left.
