open System

module ToyRobot =

    [<AutoOpen>]
    module Types =

        type Direction =
            | North
            | South
            | East
            | West

        type Transform =
            { X: int; Y: int; Direction: Direction }

        type Turn =
            | Left
            | Right

        type Command =
            | Place of Transform
            | Move
            | Turn of Turn
            | Report
            | AutoReport of bool
            | Empty
            | Exit
            | Reset

        type Tabletop =
            { Width: int
              Height: int
              RobotTransform: Transform option }

            static member New() =
                { Width = 4
                  Height = 4
                  RobotTransform = None }

        type Reader = unit -> string
        type Output = string -> unit

        type Context =
            { Reader: Reader
              Output: Output
              mutable AutoReport: bool
              Tabletop: Tabletop }

            static member New(reader, output) =
                { Reader = reader
                  Output = output
                  AutoReport = false
                  Tabletop = Tabletop.New() }

    let validatePosition tabletop pos =
        if pos.X >= 0 && pos.X <= tabletop.Width && pos.Y >= 0 && pos.Y <= tabletop.Height then
            Some pos
        else
            None

    let move pos =
        match pos.Direction with
        | North -> { pos with Y = pos.Y + 1 }
        | South -> { pos with Y = pos.Y - 1 }
        | East -> { pos with X = pos.X + 1 }
        | West -> { pos with X = pos.X - 1 }


    let turn d pos =
        match d with
        | Left ->
            match pos.Direction with
            | North -> { pos with Direction = West }
            | West -> { pos with Direction = South }
            | South -> { pos with Direction = East }
            | East -> { pos with Direction = North }
        | Right ->
            match pos.Direction with
            | North -> { pos with Direction = East }
            | West -> { pos with Direction = North }
            | South -> { pos with Direction = West }
            | East -> { pos with Direction = South }

    let report ctx pos =
        let out = sprintf "%d,%d,%A" pos.X pos.Y pos.Direction
        ctx.Output(out.ToUpperInvariant()) |> ignore

    let parseLine (s: string) =
        if String.IsNullOrWhiteSpace(s) then
            Empty |> Ok
        else
            let parts = s.Trim().ToUpperInvariant().Split(' ')

            match parts.[0] with
            | "PLACE" ->
                try
                    let args = parts.[1].Split(',')

                    if args.Length <> 3 then
                        failwith "incorrect syntax"

                    let x = int args[0]
                    let y = int args[1]

                    if x < 0 || y < 0 then
                        failwith "Invalid position"

                    let d =
                        match args.[2] with
                        | "NORTH" -> North
                        | "SOUTH" -> South
                        | "EAST" -> East
                        | "WEST" -> West
                        | _ -> failwith "Invalid direction"

                    Place { X = x; Y = y; Direction = d } |> Ok
                with e ->
                    $"Invalid PLACE command: {e.Message}" |> Error

            | "MOVE" -> Move |> Ok
            | "LEFT" -> Turn Left |> Ok
            | "RIGHT" -> Turn Right |> Ok
            | "REPORT" -> Report |> Ok
            | "AUTOREPORT" when parts.Length = 2 ->
                match parts.[1] with
                | "ON" -> AutoReport true |> Ok
                | "OFF" -> AutoReport false |> Ok
                | _ -> "Invalid AUTOREPORT command" |> Error
            | "EXIT" -> Exit |> Ok
            | "RESET" -> Reset |> Ok
            | s when String.IsNullOrWhiteSpace(s) -> Empty |> Ok
            | _ -> "Invalid command" |> Error

    let processCommand (ctx: Context) tabletop cmd =
        let newTransform =
            match cmd, tabletop.RobotTransform with
            | Place xform, _ -> Some xform
            | Move, Some pos -> Some(move pos)
            | Turn dir, Some pos -> Some(turn dir pos)
            | Report, Some pos ->
                report ctx pos
                Some pos
            | AutoReport on, _ ->
                ctx.AutoReport <- on
                ctx.Output(sprintf "Autoreport set to %b" on)
                ctx.Tabletop.RobotTransform
            | _ -> None
            |> Option.bind (validatePosition tabletop)
            |> Option.orElse tabletop.RobotTransform // keep the current position if the new one is invalid

        if ctx.AutoReport && Option.isSome newTransform then
            report ctx newTransform.Value

        { tabletop with
            RobotTransform = newTransform }


    let processCommands ctx =
        let rec loop tabletop =
            let r = parseLine (ctx.Reader())

            match r with
            | Error e ->
                ctx.Output(e)
                tabletop
            | Ok cmd ->
                match cmd with
                | Exit -> tabletop
                | Reset -> loop (Tabletop.New())
                | cmd -> loop (processCommand ctx tabletop cmd)

        loop (Tabletop.New())

#if !TESTS
module Tests =
    open Expecto

    let simulateCommands commands =
        let inputs = commands |> List.toArray
        let mutable index = 0

        fun () ->
            if index < inputs.Length then
                let result = inputs.[index]
                index <- index + 1
                result
            else
                ""

    let captureOutput () =
        let outputBuffer = System.Text.StringBuilder()
        (fun s -> outputBuffer.Append(s + "\n") |> ignore), (fun () -> outputBuffer.ToString())

    [<Tests>]
    let toyRobotTests =
        testList
            "ToyRobot Tests"
            [

              test "Robot should move north when placed at (0,0) facing North" {
                  let reader = simulateCommands [ "PLACE 0,0,NORTH"; "MOVE"; "REPORT"; "EXIT" ]
                  let capture, getOutput = captureOutput ()
                  let ctx = ToyRobot.Types.Context.New(reader, capture)

                  ToyRobot.processCommands ctx |> ignore

                  Expect.equal (getOutput ()) "0,1,NORTH\n" "Robot should move to (0,1) facing North"
              }

              test "Robot should rotate left when placed at (0,0) facing North" {
                  let reader = simulateCommands [ "PLACE 0,0,NORTH"; "LEFT"; "REPORT"; "EXIT" ]
                  let capture, getOutput = captureOutput ()
                  let ctx = ToyRobot.Types.Context.New(reader, capture)

                  ToyRobot.processCommands ctx |> ignore

                  Expect.equal (getOutput ()) "0,0,WEST\n" "Robot should rotate left to face West"
              }

              test "Robot should not move beyond the tabletop edges" {
                  let reader = simulateCommands [ "PLACE 0,0,SOUTH"; "MOVE"; "REPORT"; "EXIT" ]
                  let capture, getOutput = captureOutput ()
                  let ctx = ToyRobot.Types.Context.New(reader, capture)

                  ToyRobot.processCommands ctx |> ignore

                  Expect.equal
                      (getOutput ())
                      "0,0,SOUTH\n"
                      "Robot should stay in place at (0,0) when attempting to move South"
              }

              test "Robot should not error when placed in an invalid position" {
                  let reader = simulateCommands [ "PLACE 0,5,NORTH"; "REPORT"; "EXIT" ]
                  let capture, getOutput = captureOutput ()
                  let ctx = ToyRobot.Types.Context.New(reader, capture)

                  ToyRobot.processCommands ctx |> ignore

                  Expect.stringContains (getOutput ()) "" "Should return error for out of bounds PLACE command"
              }

              test "Robot should ignore any commands before a valid PLACE command" {
                  let reader = simulateCommands [ "MOVE"; "LEFT"; "REPORT"; "EXIT" ]
                  let capture, getOutput = captureOutput ()
                  let ctx = ToyRobot.Types.Context.New(reader, capture)

                  ToyRobot.processCommands ctx |> ignore

                  Expect.stringContains (getOutput ()) "" "Should ignore commands before a valid PLACE command"
              }

              test "Robot should ignore invalid commands" {
                  let reader = simulateCommands [ "PLACE 0,0,NORTH"; "INVALID"; "REPORT"; "EXIT" ]
                  let capture, getOutput = captureOutput ()
                  let ctx = ToyRobot.Types.Context.New(reader, capture)

                  ToyRobot.processCommands ctx |> ignore

                  Expect.stringContains (getOutput ()) "" "Should ignore invalid commands"
              }

              test "Robot should ignore empty commands" {
                  let reader = simulateCommands [ ""; "PLACE 0,0,NORTH"; ""; "REPORT"; "EXIT" ]
                  let capture, getOutput = captureOutput ()
                  let ctx = ToyRobot.Types.Context.New(reader, capture)

                  ToyRobot.processCommands ctx |> ignore

                  Expect.equal (getOutput ()) "0,0,NORTH\n" "Should ignore empty commands"
              }

              test "Robot should ignore non ascii commands" {
                  let reader = simulateCommands [ "PLACE 0,0,NORTH"; "😀"; "REPORT"; "EXIT" ]
                  let capture, getOutput = captureOutput ()
                  let ctx = ToyRobot.Types.Context.New(reader, capture)

                  ToyRobot.processCommands ctx |> ignore

                  Expect.equal (getOutput ()) "Invalid command\n" "Should ignore non ascii commands"
              }

              test "Robot should report its position when requested" {
                  let reader = simulateCommands [ "PLACE 0,0,NORTH"; "REPORT"; "EXIT" ]
                  let capture, getOutput = captureOutput ()
                  let ctx = ToyRobot.Types.Context.New(reader, capture)

                  ToyRobot.processCommands ctx |> ignore

                  Expect.equal (getOutput ()) "0,0,NORTH\n" "Robot should report its position"
              }

              ]
#endif

open ToyRobot
open Expecto
// Main entry point
[<EntryPoint>]
let main argv =
#if !TESTS
    if argv |> Array.exists (fun arg -> arg = "--run-tests") then
        printfn "Running tests..."
        // Run the tests
        runTestsInAssemblyWithCLIArgs [] argv[1..]
    else
#endif
    // Normal execution of the ToyRobot program
    let ctx = ToyRobot.Types.Context.New(Console.ReadLine, Console.WriteLine)
    ToyRobot.processCommands ctx |> ignore
    0
