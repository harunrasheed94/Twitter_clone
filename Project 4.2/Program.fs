open Suave
open Suave.Http
open Suave.Operators
open Suave.Filters
open Suave.Successful
open Suave.Files
open Suave.RequestErrors
open Suave.Logging
open Suave.Utils
open System
open System.Net
open System.Collections.Generic
open Suave.Sockets
open Suave.Sockets.Control
open Suave.WebSocket
open System.Text.RegularExpressions

let allActorDict = new Dictionary<string, WebSocket>()
let allNameDict = new Dictionary<WebSocket, string>()
let actorTweets = new Dictionary<WebSocket, List<string>>()
let mySubscriber = new Dictionary<WebSocket, List<WebSocket>>()
let mentionedList=new Dictionary<WebSocket, List<string>>()
let hashTagList=new Dictionary<string, List<string>>()
let mySubscription=new Dictionary<WebSocket, List<string>>()



let ws (webSocket : WebSocket) (context: HttpContext) =
  socket {
    // if `loop` is set to false, the server will stop receiving messages
    let mutable loop = true
    //let h={defaultRegister with Message="Registered"}
    while loop do
      // the server will wait for a message to be received without blocking the thread
      let! msg = webSocket.read()

      match msg with
      // the message has type (Opcode * byte [] * bool)
      //
      // Opcode type:
      //   type Opcode = Continuation | Text | Binary | Reserved | Close | Ping | Pong
      //
      // byte [] contains the actual message
      //
      // the last element is the FIN byte, explained later
      | (Text, data, true) ->
        // the message can be converted to a string
        let str = UTF8.toString data
        
        let strSplit=str.Split '&'
        let functionality=strSplit.[0]
        let clientStr=strSplit.[1]
        
        let mutable response=""
        match functionality with 
          | "Register"->  
                        printfn "Register Server has been called %s %s" functionality clientStr
                        response<-"Username "+clientStr+" has been Succesfully logged in"
                        if allActorDict.ContainsKey(clientStr) then 
                             response<-"Welcome Back "+clientStr+", You are already a user in twitter"
                        else
                             allActorDict.Add(clientStr,webSocket)
                             actorTweets.Add(webSocket,new List<string>())
                             mySubscriber.Add(webSocket,new List<WebSocket>())
                             mentionedList.Add(webSocket,new List<string>())   
                             mySubscription.Add(webSocket,new List<string>())
                             allNameDict.Add(webSocket,clientStr)
                        let byteResponse =
                                  response
                                  |> System.Text.Encoding.ASCII.GetBytes
                                  |> ByteSegment

                        // the `send` function sends a message back to the client
                        do! webSocket.send Text byteResponse true
          | "Tweet"-> 
                    printfn "Tweet Server has been called %s %s" functionality clientStr
                    let key,str=actorTweets.TryGetValue webSocket
                    str.Add(clientStr)
                    
                    response<-"Tweet by you: "+clientStr
                    let byteResponse =
                                  response
                                  |> System.Text.Encoding.ASCII.GetBytes
                                  |> ByteSegment

                        // the `send` function sends a message back to the client
                    do! webSocket.send Text byteResponse true
                    
                    let key1,sockets=mySubscriber.TryGetValue webSocket
                    
                    for item in sockets do
                       response<-"Your Subscriber "+allNameDict.Item(webSocket)+" has Tweeted : "+clientStr
                       let byteResponse =
                                  response
                                  |> System.Text.Encoding.ASCII.GetBytes
                                  |> ByteSegment

                        // the `send` function sends a message back to the client
                       do! item.send Text byteResponse true

                     //HashTag Functionality
                    let hashRegex = new Regex(@"#\w+");
                    let hashMatches = hashRegex.Matches clientStr
                    for i in 0..hashMatches.Count-1 do
                      let hashTag=hashMatches.Item i|> string
                      if hashTagList.ContainsKey(hashTag) then 
                          let key,str=hashTagList.TryGetValue hashTag
                          str.Add(clientStr)
                          printf "\nAdded HashTag in Existing entry\n"
                      else
                          let str=new List<string>()
                          str.Add(clientStr)
                          hashTagList.Add(hashTag,str)
                          printf "\nAdded HashTag in New entry\n";


                     //Mentioning Functionality
                    let mentionRegex = new Regex(@"@\w+");
                    let mentionMatches = mentionRegex.Matches clientStr
                    for i in 0..mentionMatches.Count-1 do
                      let mentionTag=mentionMatches.Item i|> string
                      let mentionedWebSocket=allActorDict.Item(mentionTag.[1..])
                      
                      let key,str=mentionedList.TryGetValue mentionedWebSocket
                      str.Add(clientStr)
                      response<-"You have been mentioned in a tweet by "+allNameDict.Item(webSocket)+". The tweet is : "+clientStr
                      let byteResponse =
                                  response
                                  |> System.Text.Encoding.ASCII.GetBytes
                                  |> ByteSegment
                      do! mentionedWebSocket.send Text byteResponse true
                      
           | "ReTweet"-> 
                    printfn "ReTweet Server has been called %s %s" functionality clientStr
                    let key,str=actorTweets.TryGetValue webSocket
                    str.Add(clientStr)
                    response<-"You have ReTweeted : "+clientStr
                    let byteResponse =
                                  response
                                  |> System.Text.Encoding.ASCII.GetBytes
                                  |> ByteSegment

                        // the `send` function sends a message back to the client
                    do! webSocket.send Text byteResponse true
           | "Subscribe"-> 
                    printfn "Subscribe Server has been called %s %s" functionality clientStr
                    let subscriberWebSocket=allActorDict.Item(clientStr)
                    let mutable key,str=mySubscription.TryGetValue webSocket
                    str.Add(clientStr)
                    let keySub,strSub=mySubscriber.TryGetValue subscriberWebSocket
                    strSub.Add(webSocket)
                    response<-"You have successfully subscribed to: "+clientStr
                    let byteResponse =
                                  response
                                  |> System.Text.Encoding.ASCII.GetBytes
                                  |> ByteSegment
                    do! webSocket.send Text byteResponse true
                    response<-allNameDict.Item(webSocket)+" has subscribed to you"
                    let byteResponse =
                                  response
                                  |> System.Text.Encoding.ASCII.GetBytes
                                  |> ByteSegment
                    do! subscriberWebSocket.send Text byteResponse true
          | "HomePage" -> 
                    printfn "showing home page"
                    let key1,clientList=mySubscription.TryGetValue webSocket
                    
                    for item in clientList do
                       let k,socket= allActorDict.TryGetValue item
                       let k1,tweetList= actorTweets.TryGetValue socket
                       let tstring=tweetList.Item(tweetList.Count-1)
                       response<-"Your Subcriber "+item+"'s latest tweet : "+tstring
                       let byteResponse =
                                  response
                                  |> System.Text.Encoding.ASCII.GetBytes
                                  |> ByteSegment

                        // the `send` function sends a message back to the client
                       do! webSocket.send Text byteResponse true
          | "QueryMention"->
                    printfn "Sending back all the tweets the user is mentioned in"
                    let k,tweetList= mentionedList.TryGetValue webSocket
                    for item in tweetList do
                        response<-item
                        let byteResponse =
                                  response
                                  |> System.Text.Encoding.ASCII.GetBytes
                                  |> ByteSegment

                        // the `send` function sends a message back to the client
                        do! webSocket.send Text byteResponse true
          | "QueryHash"->
                    if hashTagList.ContainsKey(clientStr) then 
                        let k,tweetList = hashTagList.TryGetValue clientStr
                        for item in tweetList do
                            response<-item
                            let byteResponse =
                                  response
                                  |> System.Text.Encoding.ASCII.GetBytes
                                  |> ByteSegment

                            // the `send` function sends a message back to the client
                            do! webSocket.send Text byteResponse true
                    else
                        response<-"No Tag found"
                        let byteResponse =
                                  response
                                  |> System.Text.Encoding.ASCII.GetBytes
                                  |> ByteSegment

                            // the `send` function sends a message back to the client
                        do! webSocket.send Text byteResponse true
          | _->()

        
        
        // the response needs to be converted to a ByteSegment
        

      | (Close, _, _) ->
        let emptyResponse = [||] |> ByteSegment
        do! webSocket.send Close emptyResponse true

        // after sending a Close message, stop the loop
        loop <- false

      | _ -> ()
    }

/// An example of explictly fetching websocket errors and handling them in your codebase.
let wsWithErrorHandling (webSocket : WebSocket) (context: HttpContext) = 
   
   let exampleDisposableResource = { new IDisposable with member __.Dispose() = printfn "Resource needed by websocket connection disposed" }
   let websocketWorkflow = ws webSocket context
   
   async {
    let! successOrError = websocketWorkflow
    match successOrError with
    // Success case
    | Choice1Of2() -> ()
    // Error case
    | Choice2Of2(error) ->
        // Example error handling logic here
        printfn "Error: [%A]" error
        exampleDisposableResource.Dispose()
        
    return successOrError
   }

let app : WebPart = 
  choose [
    path "/websocket" >=> handShake ws
    path "/websocketWithSubprotocol" >=> handShakeWithSubprotocol (chooseSubprotocol "test") ws
    path "/websocketWithError" >=> handShake wsWithErrorHandling    
    GET >=> choose [ path "/" >=> file "homepage.html"; browseHome]
    GET >=> choose [ path "/actions.js" >=> file "actions.js"]
    GET >=> choose [ path "/styles.css" >=> file "styles.css"]
    GET >=> choose [ path "/tweetImg.jpg" >=> file "tweetImg.jpg"]
    NOT_FOUND "Found no handlers." ]

let myCfg =
  { defaultConfig with
      bindings = [ 
                   
                   HttpBinding.createSimple HTTP "127.0.0.1" 8080
                  
                  ]
    }
[<EntryPoint>]
let main _ =
 
  startWebServer myCfg app
  
  0

//
// The FIN byte:
//
// A single message can be sent separated by fragments. The FIN byte indicates the final fragment. Fragments
//
// As an example, this is valid code, and will send only one message to the client:
//
// do! webSocket.send Text firstPart false
// do! webSocket.send Continuation secondPart false
// do! webSocket.send Continuation thirdPart true
//
// More information on the WebSocket protocol can be found at: https://tools.ietf.org/html/rfc6455#page-34
//