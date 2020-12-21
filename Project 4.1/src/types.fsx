open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open System.Collections.Generic

type registrationRecord = {
    Client:int;
    Message: string;
    SubscriberCnt:int;
    ClientStr:string;
    ActorObj:ActorSelection
}

type subscribeUsersAdd = {
    ClientName:string;
    SubscriberName:string;
    Message: string
}

type tweetRecord = {
    ClientName:string;
    Message: string;
    Tweet: string; 
}

type reTweetRecord = {
    ClientName:string;
    Message: string;
}

type printRecord = {
    Client:string;
    Message:string;
}

type queryRecord = {
    ClientName:string;
    Message:string;
    HashTag:string;
    ListTweets:List<string>;
}

let defaultregistrationRecord = {Client=0; Message = "default"; SubscriberCnt = -1; ClientStr = "default"; ActorObj = null}
let defaultsubscribeUsersAdd = {ClientName="-1"; SubscriberName="-1"; Message = "default"}
let defaultTweetRecord = {ClientName = "-1"; Message="default";Tweet="-1"}
let defaultReTweetRecord = {ClientName = "-1"; Message = "default"}
let defaultPrintRecord = {Client = ""; Message = ""}
let defaultqueryRecord = {ClientName = ""; HashTag = ""; Message = "";ListTweets = null}