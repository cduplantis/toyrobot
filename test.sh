#!/bin/bash

# Input data (ToyRobot commands)
input_data=$(cat <<'EOF'
PLACE 0,0,NORTH
MOVE
REPORT

PLACE 0,0,NORTH
LEFT
REPORT

PLACE 1,2,EAST
MOVE
MOVE
LEFT
MOVE
REPORT

PLACE 1,2,EAST
MOVE
MOVE
MOVE
RIGHT
MOVE
REPORT

PLACE 0,0,SOUTH
MOVE
REPORT

EXIT
EOF
)

# Expected output data
expected_output=$(cat <<'EOF'
0,1,NORTH
0,0,WEST
3,3,NORTH
4,1,SOUTH
0,0,SOUTH
EOF
)

# Compare actual output to expected output using diff
if diff <(echo "$input_data" | dotnet run) <(echo "$expected_output"); then
  echo "Test passed: Actual output matches expected output"
else
  echo "Test failed: Actual output does not match expected output"
fi
