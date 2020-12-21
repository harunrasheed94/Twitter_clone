#r "nuget: Akka.FSharp" 
#r "nuget: Akka.TestKit" 
#r "nuget: Akka.Remote"
#r "nuget: Akka.Serialization.Hyperion"
#load @"./types.fsx"

open System
open System.Threading
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open Types
open System.Collections.Generic
open System.Text.RegularExpressions

let userDict = new Dictionary<string, ActorSelection>() //clients and their actionselection objects 
let tweetsDict = new Dictionary<string, List<string>>() //clients and their respective tweets
let subscriberDict = new Dictionary<string, List<string>>() //clients and their respective tweets
let hashtagsDict = new Dictionary<string, List<string>>() //hashtags and all tweets of each hashtag
let hashtagsUserDict = new Dictionary<string,List<string>>()//clients and the hashtags tweeted by them 
let mentionsDict = new Dictionary<string, List<string>>() //clients and all the tweets they have been mentioned
let tweetsSubscribedToDict = new Dictionary<string, List<string>>()//clients and all the tweets they have subscribed to 
let mutable noofrequests = 0

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
            actor.provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
            remote.helios.tcp {
                hostname = ""localhost""
                port = 9000
            }
        }")

let system = ActorSystem.Create("Twitter",configuration)

type Server () =
    inherit Actor()
    
    override x.OnReceive(msg) =  
        noofrequests <- noofrequests + 1
        match msg with 
        | :? registrationRecord as msg  -> //registering a client in the server
               let message = msg.Message
               match message with 
               | "Client Initialization" -> 
                     let registerObj = msg
                     let clientStr = registerObj.ClientStr
                     let clientObj = registerObj.ActorObj
                     userDict.Add(clientStr,clientObj)
                     let subscriberList = new List<string>()
                     let tweetsList = new List<string>()
                     let tweetsSubscribedToList = new List<string>()
                     let mentionedTweetsList = new List<string>()
                     let hashtagUserList = new List<string>()
                     subscriberDict.Add(clientStr,subscriberList)
                     tweetsDict.Add(clientStr,tweetsList)
                     tweetsSubscribedToDict.Add(clientStr,tweetsSubscribedToList)
                     mentionsDict.Add(clientStr,mentionedTweetsList)
                     hashtagsUserDict.Add(clientStr,hashtagUserList)
                     printfn "%s Registered" clientStr
                     let registerRetObj = {defaultregistrationRecord with Message = "Registered"}
                     clientObj <! registerRetObj
               | _-> ()     
        | :? subscribeUsersAdd as msg -> //Subscribing a user to another user
               let message = msg.Message
               //printfn "Coming %A" msg
               match message with
               | "Subscribe" ->
                     let clientStr = msg.ClientName
                     let subscriberName = msg.SubscriberName
                     let key,actorSelectionObj = userDict.TryGetValue clientStr
                     let client,subscriberList = subscriberDict.TryGetValue clientStr
                     if not(subscriberList.Contains(subscriberName)) then //add the user to the subscriber list if he is not there
                       subscriberList.Add(subscriberName)
                     let subscriberRetObj = {msg with Message = "Subscribed"} 
                     printfn "%s subscribed to %s" subscriberName clientStr
                     actorSelectionObj <! subscriberRetObj 
               | _-> ()
        | :? tweetRecord as msg -> //Find the type of tweet if its normal tweet or mention
               let tweet = msg.Tweet
               let clientTweeting = msg.ClientName 
               let words = tweet.Split [|' '|]
               let mutable messageType = "NormalTweet"
               let clientsMentionedList = new List<string>()
               
               for word in words do
               //if it's a mention then add the mentioned users to the list
                  if Regex.IsMatch(word, "^(@)([a-zA-Z])+") then
                     clientsMentionedList.Add(word.Remove(0,1))
                     messageType <- "Mentions"
                 //if it's a hashtag then add the hashtag and the tweet to the hashTagsDict and hashTagsUserDict    
                  else if Regex.IsMatch(word, "^(#)([a-zA-Z0-9])+") then
                     let hashTag = word
                     if not(hashtagsDict.ContainsKey(hashTag)) then
                        hashtagsDict.Add(hashTag,new List<string>())
                     let htkey,hashTagList = hashtagsDict.TryGetValue hashTag
                     hashTagList.Add(tweet)
                     let htUserKey,hashTagUserList = hashtagsUserDict.TryGetValue clientTweeting
                     hashTagUserList.Add(tweet)

               //add this tweet to the user's tweetList in tweetsDict                           
               let key,tweetsList = tweetsDict.TryGetValue clientTweeting
               tweetsList.Add(tweet)  

               match messageType with
               | "NormalTweet" -> 
             //send the tweet to all the subscribers and add this tweet to the list of tweets the subscribers have subscribed to
                     let key1,subscriberList =  subscriberDict.TryGetValue clientTweeting
                     for subscriber in subscriberList do
                        let tweetToBeSent = {msg with Message = "ReceiveTweets"}
                        let key2,actorSelectionObj = userDict.TryGetValue subscriber
                        let key3,subscribedToTweetsList = tweetsSubscribedToDict.TryGetValue subscriber 
                        subscribedToTweetsList.Add(tweet)
                       // printfn "%A" subscribedToTweetsList
                        actorSelectionObj <! tweetToBeSent
                        printfn "%s's Tweet \"%s\" sent to %s"  clientTweeting tweet subscriber
                | "Mentions" -> //send tweets to only the mentioned users
                     for mentionedClient in  clientsMentionedList do
                        let mentionKey,mentionedList = mentionsDict.TryGetValue mentionedClient
                        let key2,actorSelectionObj = userDict.TryGetValue mentionedClient
                        mentionedList.Add(tweet)
                        let mentionTweetToBeSent = {msg with Message = "ReceiveMentionedTweets"}
                        actorSelectionObj <! mentionTweetToBeSent
                        printfn "%s has been mentioned in Tweet \"%s\" of %s. Thus this tweet sent to %s"  mentionedClient tweet clientTweeting mentionedClient

               | _->()
        | :? reTweetRecord as msg ->
               let clientReTweeting = msg.ClientName
               let key,subscribedTweetsList = tweetsSubscribedToDict.TryGetValue clientReTweeting
               let subscribedTweetsArray = subscribedTweetsList.ToArray()
               let length =  subscribedTweetsArray.Length
               let rnd = System.Random()
               if length > 0 then
                 let randomIndex = rnd.Next(0,length)
                 let mutable tweet = subscribedTweetsArray.[randomIndex] 
                 tweet <- "This is a retweet by " + clientReTweeting+" "+tweet
                 let tweetRecord = {ClientName = clientReTweeting;Message = "SendTweets"; Tweet=tweet}
                 let serverEngine = system.ActorSelection("akka.tcp://Twitter@localhost:9000/user/server1")
                 serverEngine <! tweetRecord
        | :? queryRecord as msg ->
               let message = msg.Message
               let clientName = msg.ClientName
               let hashtag = msg.HashTag
               let key2,actorSelectionObj = userDict.TryGetValue clientName
               let listofTweets = new List<string>()
               printfn "Query asked by client = %s " clientName
               let key1, subscribedToTweetsList = tweetsSubscribedToDict.TryGetValue clientName
               let key2,hashtagsList = hashtagsDict.TryGetValue hashtag
               let key3,mentionedList = mentionsDict.TryGetValue clientName
               
               if subscribedToTweetsList.Count > 0 then
                  let stringTweet = "\nAll subscribed to tweets of "+clientName+":"
                  listofTweets.Add(stringTweet)
                  for tweet in subscribedToTweetsList do
                      listofTweets.Add(tweet)

               if hashtagsList.Count > 0 then
                  let stringHT = "\nAll tweets with hashtag "+hashtag+ ":"
                  listofTweets.Add(stringHT)
                  for tweet in hashtagsList do
                      listofTweets.Add(tweet)

               if mentionedList.Count > 0 then
                  let mentionedStr = "\nAll tweets where "+clientName+" has been mentioned (My mentions):"
                  listofTweets.Add(mentionedStr)
                  for tweet in mentionedList do
                      listofTweets.Add(tweet)

               let queryRcd = {msg with ListTweets = listofTweets; Message = "ReceiveQueryResult"} 
               printfn "Sending query performance results of all subscribed tweets of %s, all tweets with hashtag %s, all tweets where %s is mentioned to %s homepage" clientName hashtag clientName clientName           
               actorSelectionObj <! queryRcd

              //  match message with
              //  | "GetAllSubscribedTweets" ->      
              //       let key,subscribedToTweetsList = tweetsSubscribedToDict.TryGetValue clientName
              //       let sendsubscribedToList = subscribedToTweetsList
              //       let queryRecord = {msg with ListTweets = sendsubscribedToList; Message = "ReceiveAllSubscribedToTweets"}
              //       actorSelectionObj <! queryRecord
              //  | "GetAllTweetswithHashtags" ->
              //       let hashtag = msg.HashTag
              //       let key,hashtagsList = hashtagsDict.TryGetValue hashtag
              //       let queryRecord = {msg with ListTweets = hashtagsList; Message = "ReceiveAllHashTagTweets"}
              //       actorSelectionObj <! queryRecord
              //  | "GetAllTweetswithMentions" ->
              //       let key,mentionedList = mentionsDict.TryGetValue clientName
              //       let queryRecord = {msg with ListTweets = mentionedList; Message = "ReceiveAllMentionedTweets"}
              //       actorSelectionObj <! queryRecord      
              //  | _-> ()     
        | :? string as msg ->
               if msg = "FinalNoofRequests" then
                  printfn "Total number of requests handled by the server engine is %d" noofrequests 
        | _-> ()

let actorRef =system.ActorOf(Props.Create(typeof<Server>), "server1")
System.Console.ReadLine() |> ignore
system.Terminate() |> ignore