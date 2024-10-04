# Toy Robot Simulator
A simple console application that simulates a toy robot moving on a square tabletop similar to the Mars Rover problem. Based of the problem description found [here](https://joneaves.wordpress.com/2014/07/21/toy-robot-coding-test/).

## Description
- The application is a simulation of a toy robot moving on a square tabletop, of dimensions 5 units x 5 units.
- There are no other obstructions on the table surface.
- The robot is free to roam around the surface of the table, but must be prevented from falling to destruction.
- Any movement that would result in the robot falling from the table must be prevented, however further valid movement commands must still be allowed.

## Installation
- Clone the repository

## Commands
- PLACE X,Y,F: Place the robot on the table at position X,Y and facing NORTH, SOUTH, EAST or WEST
- MOVE: Move the robot one unit forward in the direction it is currently facing
- LEFT: Rotate the robot 90 degrees to the left
- RIGHT: Rotate the robot 90 degrees to the right
- REPORT: Output the current position of the robot

## Example
```
PLACE 0,0,NORTH
MOVE
RIGHT
MOVE
LEFT
MOVE
RIGHT
MOVE
REPORT
```
This will output
```
2,2,EAST
```



