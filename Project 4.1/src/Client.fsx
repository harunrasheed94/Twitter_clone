#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 
#r "nuget: Akka.Remote"
#r "nuget: Akka.Serialization.Hyperion"
#load @"./types.fsx"

open System
open System.Threading
open System.Diagnostics
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Types
open System.Collections.Generic


let configuration = 
    ConfigurationFactory.ParseString(
        @"akka {
             actor.serializers{
              json  = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
              bytes = ""Akka.Serialization.ByteArraySerializer""
            }
             actor.serialization-bindings {
              ""System.Byte[]"" = bytes
              ""System.Object"" = json
            
            }
           actor {
                provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                
            }
            remote.helios.tcp {
                hostname = ""0.0.0.0""
                port = 1000
            }
        }")

let system = ActorSystem.Create ("Twitter", configuration)
let userDict = new Dictionary<string, ActorSelection>()
let allClientsList = new List<string>()
let mutable totalActors = 0 
let myClock = Stopwatch()
let mutable lastClient = ""
let hashtagsList = new List<string>()
hashtagsList.Add("#DOS_Project4")
hashtagsList.Add("#UF_GNV")
hashtagsList.Add("#CISE")
hashtagsList.Add("#COP5615")
hashtagsList.Add("#Pastry")
let allHashTagsArr = hashtagsList.ToArray()
let mutable hashTagSelected = 0

//common print function
type Print () = 
  inherit Actor()

  override x.OnReceive(msg) = 
        match msg with 
        | :? printRecord as msg-> 
            let clientName = msg.Client
            let printMessage = msg.Message 
            if clientName <> "-1" then
                printfn "%s Homepage :" clientName
            printfn "%s" printMessage
        | _->()    

let printactorRef = system.ActorOf(Props.Create(typeof<Print>), "printactor")

type Client () =
  inherit Actor() 

  let mutable clientIndex = 0
  let mutable currentSubscibersCnt = 0
  let mutable subscriberCnt = 0
  let mutable tweets = 0 
  let mutable clientStr = ""
  let mutable actorSelectionObj:ActorSelection = null

  override x.OnReceive(msg) =   
      
        match msg with  
        | :? registrationRecord as msg -> 
            let message = msg.Message
            match message with 
            | "Client Initialization" ->
                let registerObj = msg
                clientIndex <- registerObj.Client
                subscriberCnt <- registerObj.SubscriberCnt
                tweets <- registerObj.SubscriberCnt
                clientStr <- registerObj.ClientStr
                actorSelectionObj <- registerObj.ActorObj
                let serverEngine = system.ActorSelection("akka.tcp://Twitter@localhost:9000/user/server1")
                serverEngine <! registerObj
            | "Registered" ->
                let printMsg = clientStr + " has registered with the server"
                let printRecord = {Client = clientStr; Message = printMsg}
                printactorRef <! printRecord          
            | _-> ()    
        | :? subscribeUsersAdd as msg ->
             let message = msg.Message
             match message with 
             | "Subscribe"  ->  
                let serverEngine = system.ActorSelection("akka.tcp://Twitter@localhost:9000/user/server1")
                let mutable i = 0
                let mutable startIdx = clientIndex - subscriberCnt
                if startIdx < 0 then
                    startIdx <- 0

               // printfn "Subscriber list for %s is" clientStr
                while i < subscriberCnt do
                    if startIdx = clientIndex then
                       startIdx <- startIdx + 1

                    let subscriberStr = "client"+string(startIdx+1)
                    let subscriberObj = {ClientName = clientStr; SubscriberName = subscriberStr;Message = "Subscribe"}
                    startIdx <- startIdx + 1
                    serverEngine <! subscriberObj 
                    i <- i+1  
                // serverEngine <! msg  
             | "Subscribed" -> 
                let clientName = msg.ClientName
                let subscriberName = msg.SubscriberName
                let printMsg = subscriberName+" subscribed to "+clientName
                let printRecord = {Client = clientName; Message = printMsg}
                printactorRef <! printRecord  
             | _-> ()
        | :? tweetRecord as msg -> 
             let message = msg.Message
             match message with 
             | "SendTweets" -> //send tweets to all my subscribers
                let tweetMessage = "Hi this is "+clientStr+ " sending hi to all my subscribers"
                let tweetRecord = {ClientName = clientStr; Message ="SendTweets";Tweet = tweetMessage}
                let serverEngine = system.ActorSelection("akka.tcp://Twitter@localhost:9000/user/server1")
                serverEngine <! tweetRecord
             | "SendTweetswithHashtags" -> //send tweets with hashtags to all my subscribers
                hashTagSelected <- (hashTagSelected + 1) % 5
                let hashtag = allHashTagsArr.[hashTagSelected]
                let tweetMessage = hashtag+" "+clientStr+" tweet by "+clientStr
                let tweetRecord = {ClientName = clientStr; Message ="SendTweetsWithHashtags";Tweet = tweetMessage}
                let serverEngine = system.ActorSelection("akka.tcp://Twitter@localhost:9000/user/server1")
                serverEngine <! tweetRecord
             | "SendTweetswithMentions" -> //send tweets with mentions to all the users mentioned
                let rnd  = System.Random()
                let noOfmentionUsers = rnd.Next(1,4)
                let allUsersArr = allClientsList.ToArray()
                let length = allUsersArr.Length
                let mentionedUsersList = new List<string>()
                let mutable i = 0

                //select random Mentioned users and add them to the mentionedUsersList
                while i < noOfmentionUsers do
                  let randomIndex = rnd.Next(0,length)
                  let user = allUsersArr.[randomIndex]
                  let userSet = Set.ofSeq mentionedUsersList
                  if (user <> clientStr && not(userSet.Contains(user))) then
                     mentionedUsersList.Add(user)
                     i <- i+1
                
                //generate the tweet message by mentioning each of the random user selected to be mentioned
                let mutable tweetMessage = "Hi "+clientStr+" mentioning"
                for user in mentionedUsersList do
                    tweetMessage <- tweetMessage + " @"+user

                //send the message to the server so as to send the tweet to the mentioned users
                let tweetRecord = {ClientName = clientStr; Message = "SendTweetsWithMentions";Tweet = tweetMessage}
                let serverEngine = system.ActorSelection("akka.tcp://Twitter@localhost:9000/user/server1")
                serverEngine <! tweetRecord    

             | "ReceiveTweets" -> //receive tweets from everyone to whom I am subscribed to
                let sender = msg.ClientName
                let tweetMessageReceived = msg.Tweet
                let printMsg = clientStr+" received the tweet \""+tweetMessageReceived+ "\" from "+sender
                let printRecord = {Client = clientStr; Message = printMsg}
                printactorRef <! printRecord 

             | "ReceiveMentionedTweets" -> //receive tweets where u have been mentioned at
                let sender = msg.ClientName
                let tweetMessageReceived = msg.Tweet
                let printMsg = sender+" mentioned "+clientStr+ " in his tweet \""+tweetMessageReceived+"\""
                let printRecord = {Client = clientStr; Message = printMsg}
                printactorRef <! printRecord 

             |_-> ()

        | :? reTweetRecord as msg -> //retweet 
             let reTweetRecord = {ClientName = clientStr; Message="ReTweet"}
             let serverEngine = system.ActorSelection("akka.tcp://Twitter@localhost:9000/user/server1")
             serverEngine <! reTweetRecord
        | :? queryRecord as msg ->
             let message = msg.Message
               
             match message with 
             | "GetAllTweetsHashtagsMentions" ->
                let serverEngine = system.ActorSelection("akka.tcp://Twitter@localhost:9000/user/server1")
                let rand = System.Random()
                let randomIndex = rand.Next(0,5)
                let hashtag = allHashTagsArr.[randomIndex]
                let queryRcd = {ClientName = clientStr;Message="GetAllTweetsHashtagsMentions";HashTag = hashtag;ListTweets=null} 
                serverEngine <! queryRcd
  
             | "ReceiveQueryResult" ->
                  let tweetsList = msg.ListTweets
                  
                  let printRcd = {Client = "\n\n"+clientStr; Message = "Query Results of "+clientStr}
                  printactorRef <! printRcd
                  for tweet in tweetsList do
                      let printRcd = {Client = "-1"; Message = tweet}
                      printactorRef <! printRcd

                  if lastClient = clientStr then 
                      let printRcd1 = {Client = "-1"; Message = "\n\n\n ----End of Simulation---- \n\n\n"}
                      printactorRef <! printRcd1
                      Thread.Sleep(20)
                      let n = totalActors
                      let timeElapsed = myClock.ElapsedMilliseconds
                      let timeElapsedStr = "Total time for simulation is "+string(timeElapsed)+" ms"
                      let printRcd2 = {Client = "-1"; Message = timeElapsedStr}
                      printactorRef <! printRcd2
                      let serverEngine = system.ActorSelection("akka.tcp://Twitter@localhost:9000/user/server1")
                      serverEngine <! "FinalNoofRequests"
             | _-> () 
        |_-> ()

let getZipFConstantForActors total =  
    let mutable s:float = 0.0
    for i in 1..total do
        s <- s + (1.0/(i |> float))
    let finalRes  = Math.Pow(s,(-1|>float))
    finalRes

let zipfDistribution i zipfConst total = 
    let subCnt  =  Math.Round((zipfConst)/(i |> float) * (total|>float)) |>int
    subCnt

let init param = 
  
    let n, maxSubCnt = param
    totalActors <- n
    let rnd  = System.Random()
    myClock.Start()
    let zipfConstant  =  getZipFConstantForActors n
//spawn client actors 
    for i in [0 .. n-1] do
        let clientStr = "client"+string(i+1)
        allClientsList.Add(clientStr)
        let actorRef = system.ActorOf(Props.Create(typeof<Client>), clientStr)
        let actorSelect = "akka.tcp://Twitter@0.0.0.0:1000/user/"+clientStr
        let client1 = system.ActorSelection(actorSelect)
        //printfn "%A" client1
        userDict.Add(clientStr,client1)

    Thread.Sleep(30)
    printfn "Client registration begin"
  //set subscribercnt and name for each client and register each client with server 
    for i in [0 .. n-1] do
        let clientStr = "client"+string(i+1)
        let clientName,actorSelection = userDict.TryGetValue clientStr
        let subsciberCnt = zipfDistribution (i+1) zipfConstant n 
        let clientRegister = {Client = i; Message = "Client Initialization";SubscriberCnt = subsciberCnt;ClientStr = clientStr;ActorObj = actorSelection}
        actorSelection <! clientRegister
        Thread.Sleep(10)
    
  //Thread.Sleep(n*30)
    printfn "Simulation of Client Subscription"
//set subscribers for each client    
    for i in [0 .. n-1] do
        let clientName, actorSelection = userDict.TryGetValue ("client"+string(i+1)) 
        let defaultSubscribeRegister = {defaultsubscribeUsersAdd with Message = "Subscribe"}
        actorSelection <! defaultSubscribeRegister
        Thread.Sleep(10)
    
    //Thread.Sleep(n*30)
    printfn "Simulation of tweets"
 //simulate Normal Tweets
    for i in [0 .. n-1] do
        let clientName, actorSelection = userDict.TryGetValue ("client"+string(i+1)) 
        let tweetRecord = {defaultTweetRecord with Message = "SendTweets"}
        actorSelection <! tweetRecord
        Thread.Sleep(10)

    //Thread.Sleep(n*40)
    printfn "Simulation of tweets with hashtags"
   //simulate tweets with hashtags
    for i in [0 .. n-1] do
        let clientName, actorSelection = userDict.TryGetValue ("client"+string(i+1))
        let tweetRecord = {defaultTweetRecord with Message = "SendTweetswithHashtags"}
        actorSelection <! tweetRecord
        Thread.Sleep(10)

    //Thread.Sleep(n*50)
    printfn "Simulation of tweets with mentions"
    //simulate tweets with mentions
    for i in [0 .. n-1] do
        let clientName, actorSelection = userDict.TryGetValue ("client"+string(i+1))
        let tweetRecord = {defaultTweetRecord with Message = "SendTweetswithMentions"}
        actorSelection <! tweetRecord
        Thread.Sleep(10)

    //Thread.Sleep(n*60)
    printfn "Simulation of retweets"

    for i in [0 .. n-1] do
        let clientName, actorSelection = userDict.TryGetValue ("client"+string(i+1))
        let retweetRecord = {defaultReTweetRecord with Message = "ReTweet"}
        actorSelection <! retweetRecord
        Thread.Sleep(10)
    
    //Thread.Sleep(n*60)
    let connectedList = new List<string>()
    let connectedUsers = n/2;
    let mutable j = 0
    let allUsersArr = allClientsList.ToArray()

    while j < connectedUsers do
        let randomIndex = rnd.Next(0,n)
        let user = allUsersArr.[randomIndex]
        let userSet = Set.ofSeq connectedList
        if not(userSet.Contains(user)) then
            connectedList.Add(user)
            j <- j+1

    //Thread.Sleep(n*80)        
    for i in [0..n-1] do
        let user = allUsersArr.[i]
        if not(connectedList.Contains(user)) then
            let msg = user + " has disconnected"
            let printRecord = {Client = user;Message = msg}
            printactorRef <! printRecord
            
    //Thread.Sleep(n*120)
    let mutable j = 0
    for user in connectedList do
        let clientName, actorSelection = userDict.TryGetValue user
        if j = (connectedList.Count - 1) then
           lastClient <- user 
        let queryRcd1 = {defaultqueryRecord with Message = "GetAllTweetsHashtagsMentions"}
        actorSelection <! queryRcd1
        j <- j + 1
        Thread.Sleep(30)  
        
let args : string array = fsi.CommandLineArgs |> Array.tail
let mutable nActors = args.[0] |> int
let mutable maximumSubCnt = 5
if nActors <= 5 then
   maximumSubCnt <- nActors-1
init(nActors,maximumSubCnt) 
          
System.Console.ReadLine() |> ignore
system.Terminate() |> ignore
