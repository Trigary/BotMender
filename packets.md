# Packet documentation

A place where all of the packets are explained.  
General rules:
 - All packets' names follow the format "Channel_Sender_Category_[Subcategories]_Name"
 - The "Channel" is either "Tcp" or "Udp" depending on which socket is used to transmit the packet.
 - The "Sender" can be "Client", "Server" or "Both" depending on which parties are allowed to send the packet.
 - The "Category" and the optional "Subcategories" are one/multiple of the categories present in this documentation.
 - The "Name" is the exact name of the packet.

## Movement

**Udp_Client_Movement_InputUpdate: the client informs the server of its current input.**  
8 bits: rightward | leftward | upward | downward | forward | backward | nothing | nothing  
Each bit represents whether the specified axis is pressed or not.  

**Udp_Server_Movement_StateUpdate: the server informs the client of X many bots' states.**  
X * 54 byte: Byte-BotId, Byte-Input, Vector3-Position, Vector4-Rotation, Vector3-Velocity, Vector3-AngularVelocity  
The input byte's format is specified in the "Udp_Client_Movement_InputUpdate" packet.  
