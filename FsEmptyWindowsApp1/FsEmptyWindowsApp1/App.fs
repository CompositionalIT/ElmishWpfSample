open Elmish
open Elmish.WPF
open FsXaml
open System

type MainWindow = XAML<"MainWindow.xaml">

/// The model bound to the view
type Model =
    { Name : string
      Description : string
      Time : DateTime
      Trace : string list }

/// Different messages that can flow through the system.
type Msg =
    | HitMe
    | Trace of Msg
    | Time of DateTime
    | Name of string
    | Description of string

[<AutoOpen>]
/// Contains code for event-driven subscriptions.
module Subscriptions =
    /// Sends a message every 1 second to set the Time.
    let updateTime dispatch = async {
        while true do
            do! Async.Sleep 1000
            dispatch (Time DateTime.UtcNow) } |> Async.Start

    let newTrace = Event<_>()
    /// Hooks in an event handler to the Trace message.
    let setUpTracing dispatch = newTrace.Publish.Add(Trace >> dispatch)

/// Start state
let init _ = { Name = "Isaac"; Description = "Test"; Time = DateTime.UtcNow; Trace = [] }, Cmd.none
        
/// The main Msg loop.
let update msg m =
    match msg with
    | Time time -> { m with Time = time }, Cmd.none
    | Name name -> { m with Name = name }, Cmd.none
    | Description description -> { m with Description = description }, Cmd.none
    | Trace (Trace _) -> m, Cmd.none
    | Trace msg ->
        { m with
            Trace =
                (sprintf "%A" msg :: m.Trace)
                |> List.truncate 10 }, Cmd.none
    | HitMe -> m, [ Name (m.Name + "x"); Description "Updated!" ] |> List.map Cmd.ofMsg |> Cmd.batch

/// Hooks up WPF bindings to the model.
let view _ _ =
    [ "Name" |> Binding.twoWay (fun m -> m.Name) (fun name _ -> Name name)
      "Description" |> Binding.oneWay (fun m -> m.Description)
      "Time" |> Binding.oneWay (fun m -> m.Time)
      "Trace" |> Binding.oneWay (fun m -> m.Trace)
      "HitMe" |> Binding.cmd (fun _ -> HitMe) ]

[<STAThread; EntryPoint>]
let main _ =
    Program.mkProgram init update view
    |> Program.withSubscription (fun _ -> [ updateTime; setUpTracing ] |> List.map Cmd.ofSub |> Cmd.batch)
    |> Program.withTrace(fun msg _ -> newTrace.Trigger msg)
    |> Program.runWindow (MainWindow())