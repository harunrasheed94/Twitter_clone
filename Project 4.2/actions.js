"use strict";
console.log("coming js")
var websocketUrl = "ws://localhost:8080/websocket";
var websocket;
var displayDiv = document.getElementById("liveFeed");
var divMsg;

let bindFunction = function() {
  jQuery("#twittermaindiv").on("keyup","input[type='text'],textarea",function(){
    let thisElem = jQuery(this);
    let typedString = thisElem.val();
    let parentDiv = thisElem.closest(".inputparentdiv");
    if(typedString.length > 0){
       parentDiv.find(".buttoncls").prop("disabled",false);
    }
    else{
      parentDiv.find(".buttoncls").prop("disabled",true);
    }  
  });

  jQuery("#twittermaindiv").on("click",".Retweetbtncls",function(){
      let thisElem = jQuery(this);
      if(thisElem.hasClass("Retweetbtncls")){
        let tweet = thisElem.data('tweet');
        beginreTweeting(tweet);
      }
  })

  jQuery("#registrationInput").on("click", function () {
    let uname = jQuery("#registerInput").val();
    if(uname != undefined && uname.length > 0){
      beginRegistration();
      jQuery("#registerInput").val("");
      jQuery("#registrationInput").prop("disabled",true);
    } 
  });

  jQuery("#tweetInput").on("click", function () {
    let tweet = jQuery("#tweet").val();
    if(tweet != undefined && tweet.length > 0){
      beginTweeting();
      jQuery("#tweet").val("");
      jQuery("#tweetInput").prop("disabled",true);
    }  
  });

  jQuery("#subscribeInput").on("click", function () {
    let uname = jQuery("#subscribe").val();
    if(uname != undefined && uname.length > 0){
      beginSubscription();
      jQuery("#subscribe").val("");
      jQuery("#subscribeInput").prop("disabled",true);
    }  
  });

  jQuery("#hashtags").on("click", function () {
    let hashtag = jQuery("#hashtagqueryinput").val();
    if(hashtag != undefined && hashtag.length > 0){
      getHashtags();
      jQuery("#hashtagqueryinput").val("");
      jQuery("#hashtags").prop("disbled",true);
    }
  });

  jQuery("#mentioninput").on("click", function () {
    getMentionedTweets();
  });
}

let webSocketFunc = function() {
  websocket = new WebSocket(websocketUrl);
  websocket.onopen = function (evt) { onOpenFunc(evt) };
  websocket.onmessage = function (evt) { funcForMessaging(evt) };
  websocket.onerror = function (evt) { displayErrorMsg(evt) };
}

let beginRegistration = function() {
  var registrationString = "Register&" + jQuery("#registerInput").val();
  divMsg = "Sent server a message to register user" + registrationString.substring(registrationString.indexOf("&") + 1, registrationString.length);
  sendToServer(registrationString);
  let placeholderStr = "Please tweet here "+ jQuery("#registerInput").val();
  jQuery("#tweet").attr("placeholder",placeholderStr);
  jQuery("#registerUserDiv").hide();
  jQuery("#twitterhomepage").show();
}

let beginTweeting = function() {
  var tweetString = "Tweet&" + jQuery("#tweet").val();
  divMsg = "Your Tweet: " + tweetString.substring(tweetString.indexOf("&") + 1, tweetString.length);
  sendToServer(tweetString);
}

let beginreTweeting = function(message) {
  var retweetString = "ReTweet&" + message
  sendToServer(retweetString);
}

let beginSubscription = function() {
  var subscriberString = "Subscribe&" + jQuery("#subscribe").val();
  divMsg = "Asking the server to subscribe the user" + subscriberString.substring(subscriberString.indexOf("&") + 1, subscriberString.length);
  sendToServer(subscriberString);

}

let getHomePage = function() {
  var homepageString = "HomePage&null";
  divMsg = "All your subscribers tweets";
  sendToServer(homepageString);
}

let getMentionedTweets = function() {
  var mentionedString = "QueryMention&null";
  divMsg = "All tweets where you have been mentioned";
  sendToServer(mentionedString);
}

let getHashtags = function() {
  var hashtagString = "QueryHash&" + jQuery("#hashtagqueryinput").val();
  divMsg = "All tweets with the hashtag you searched for";
  sendToServer(hashtagString);
}

let funcForMessaging = function(evt) {
  var divString = String(evt.data);
  var isSubscriber = divString.startsWith("Your Subscriber");
  if (isSubscriber) {
    var twt = divString.substring(divString.indexOf(":") + 2, divString.length);
    retweetFeedDisplayFunction( evt.data, twt);
  } else
    feedDisplayFunction(evt.data);
}

let feedDisplayFunction = function(message) {
  let parentDiv = jQuery("#liveFeed");
  let cloneDiv = parentDiv.find(".defaultfeeddiv").clone();
  cloneDiv.removeClass("defaultfeeddiv");
  cloneDiv.find(".feedtxtspan").text(message);
  parentDiv.append(cloneDiv);
  cloneDiv.show();
}

let retweetFeedDisplayFunction = function(message,twt){
  let parentDiv = jQuery("#liveFeed");
  let cloneDiv = parentDiv.find(".defaultfeeddiv").clone();
  cloneDiv.removeClass("defaultfeeddiv");
  cloneDiv.find(".feedtxtspan").text(message);
  parentDiv.append(cloneDiv);
  let retweetButton = jQuery('<button/>',{
    text:'Retweet',
    class:'Retweetbtncls buttoncls',
  });
  retweetButton.data("tweet",twt);
  cloneDiv.after(retweetButton);
  cloneDiv.show();
}

let displayErrorMsg = function(evt) {
  feedDisplayFunction('<span class=" feedbordercls" style="color: red;">ERROR:</span> ' + evt.data);
}

let sendToServer = function(message) {
  websocket.send(message);
}

let onOpenFunc = function(evt) { }

bindFunction();
webSocketFunc();