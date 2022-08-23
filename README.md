COMP 476 Programming Assignment 2 - George Mavroeidis

WARNING: Please notify me if you wanna use my project as a reference

Professor: Kaustubha Mendhurwar
Teacher Assistant: Daniel Rinaldi

The purpose of the program was to experiment and study different pathfinding techniques and implement them in a maze-like context with a steering AI Agent.
Code uses base structure from Lab 04 (made by Daniel Rinaldi) for setting up the game loop and the graph generation, but the pathfinding was left for the student
to work on (as well as the AI Agent). 

In conjunction of the Theoretical parts, Question 1 and Question 2, the programming part contains 3 parts (R1, R2 and R3).

R1: I set up a scene (Pathfinding_Demo), where the maze-like room was the same as the drawing example shown in the assignment. The floor and nodes are black, the walls blue
and the other obstacles yellow. The AI agent is also yellow. There are three completely disjoint rooms, where each one has a smaller room that have no other
exits. For each of the large rooms, there are two doors that connect to the main hallway area that is made of two big corridors. Off one or two of these
long corridors, there is a “dead-end” corridor for each corridor. Within each large room, there is a large static convex polygonal obstacle.

The tile graph generates at least 700 nodes in the map used for pathfinding.

R2: In this same scene, A* algorithm is implemented on the pathfinding graph that displays the path from one start node to a goal node. Both are selected by left clicking on
the nodes in the graph. First pick the start, then the goal one. By clicking off the map, or anything else instead of the nodes, both are cancelled.

There are two A* algorithms in the scene: Manhattan Distance and Clusters.

Manhattan Distance: A big method called FindNodePath(...) calculates the Manhattan distance from the start node to the goal node and returns this list of nodes as a path.
Once the path is created, it is displayed by coloring the nodes and its connections. The green nodes are the path nodes, the red nodes are the nodes in the closed list
and the blue/magenta nodes are the ones on the open list that were left there when the shortest path was found. In addition, the path nodes are connected by green lines
(using the Line Renderer) and the smoothed path is in magenta lines (more on R3). Finally, the Manhattan distance is also used to ensure diagonal, or non-orthogonal,
edges between nodes have double the movement cost.

Clusters: Another similar method called FindClusterPath(..) uses the Manhattan distance to calculate the path using the clusters. Once this cluster path is used, all
of their nodes are placed in a list that is used to calculate the node path, using FindNodePath(...), to return the actual node path, similar to Manhattan, to return a similar
path, but using less nodes. Compare screenshots to see the difference.

The clusters in the scene are also transparent boxes that encapsulate the rooms and corridors. They also change colors accordingly if they are in open, closest lists or the
path list. They are only activated in the scene at runtime when the Cluster type is selected.

Important properties of the Pathfinding.cs:
- A Star Type Enum: select type of A* you want to test (check NOTE after)
- AI Agent: a reference to the agent in the scene
- Line Renderer: Normal path green line renderer
- Smoothed Line Renderer: because you can't put two same components in one object, I reference a second line renderer found in the "Obstacles" parent empty
- Current Path Index: the current path of the node in the smoothed index (used for making agent walk alongside the smoothed path)

NOTE: Don't switch between Manhattan and Cluster heuristics during runtime. To switch between them, do that offline (not at runtime).

R3: I duplicated the scene above and called this one Pathfinding_Demo_Player. In this scene, there is actually an AI agent, so the start node is
calculated by finding the nearest one to the agent. The user only selects and deselects the goal node.

Initially, the character is placed randomely in any of the small rooms and a random node from it is selected, where the player is placed.
Once the goal node is selected

This scene basically implements the AI agent using steering arrive and align behaviours to actually move along the path. Once the node path
is calculated, a basic smooth functon called SmoothPath(path) is used to smooth out the path using less nodes. A raycast is shot and determines
if the node is obstructed by an obstacle or not. If yes, it is added to the smoothedPath list, else, it is not added.

Once the smoothed path is calculated (colored in magenta) the agent uses arrive steering to arrive to each smoothed path node. The current path index
variable is incemented each time it reaches a node, until the goal node. The agent has no rigidbody or colliders properties. There is a realistic steering feel,
but there is also a kinematic movement implemented as well. Once the agent reaches the goal node, it slows down and halts on top of it.

The screenshots folder shows two fill examples between the Manhattan and Cluster pathfinding. It is obvious that the cluster approach utilizes less nodes.

George Mavroeidis
40065356
