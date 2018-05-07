# BotMender

A Unity game where bots can be built from 'blocks' and then taken to action.
The current version is an early prototype.

## Blocks, building, systems

Each blocks has mass, health and "connect-sides": sides on which it can connect to other blocks.
This system is expected to get reworked a bit due to some flaws,
because for example it is very limiting when it comes to multi-blocks.
All blocks may also define a system: if the block is placed, the system gets installed onto the bot.
There are 3 kinds of system: propulsion, weapon and "active".
An active system is usually some sort of special ability which has a cooldown.

## Bots

A bot is a collection of its parts (blocks), and systems.
The health of a bot is the sum of its blocks' health.
The position, type and rotation of a block can be serialized into 64 bits
(48 bits would also be enough, but I went with 64 for simplicity's sake),
therefore a bot structure can be serialized into a long array.