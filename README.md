# Unity-C---machine-learning-game-AI

This project is part of my machine learning AI, this forms the unity part.

This provides the client code, that connects to my python server.

It transmits the current state that the agent sees of the world, and any rewards obtained by the agent.

Then, if a message is recieved from the server, containing actions for the agent to execute, then it will
proceed to repeat that action, until either the action is complete or a new set of actions is recieved by the server.

On a state change, that information to the server, to see what its actions should be.
