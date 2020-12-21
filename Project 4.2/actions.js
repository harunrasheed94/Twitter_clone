"use strict";
console.log("coming js")
var websocketUrl = "ws://localhost:8080/websocket";
var websocket;
var displayDiv = document.getElementById("liveFeedId");
var divMsg;

let bindFunction = function() {
  jQuery("#registrationInput").on("click", function () {
    beginRegistration();
  });

  jQuery("#tweetInput").on("click", function () {
    beginTweeting();
  });

  jQuery("#subscribeInput").on("click", function () {
    beginSubscription();
  });

  jQuery("#mentioninput").on("click", function () {
    getMentionedTweets();
  });

  jQuery("#hashtags").on("click", function () {
    getHashtags();
  });
}

let webSocketFunc = function() {
  websocket = new WebSocket(websocketUrl);
  websocket.onopen = function (evt) { onOpenFunc(evt) };
  websocket.onmessage = function (evt) { funcForMessaging(evt) };
  websocket.onerror = function (evt) { displayErrorMsg(evt) };
}

let beginRegistration = function() {
  var registrationString = "Register&" + document.getElementById("registerInput").value
  divMsg = "Sent server a message to register user" + registrationString.substring(registrationString.indexOf("&") + 1, registrationString.length);
  sendToServer(registrationString);
  document.getElementById("tweet").placeholder = "Please tweet here " + document.getElementById("registerInput").value + "";
  document.getElementById("registerUserDiv").remove();
  document.getElementById("tweetDiv").style.display = 'block';
  document.getElementById("subscribeDiv").style.display = 'block';
  document.getElementById("queryDivId").style.display = 'block';
  document.getElementById("hashtagDivId").style.display = 'block';
  document.getElementById("liveFeedId").style.display = 'block';
  document.getElementById("queryperfDivId").style.display = 'block';
}

let beginTweeting = function() {
  var tweetString = "Tweet&" + document.getElementById("tweet").value
  divMsg = "Your Tweet: " + tweetString.substring(tweetString.indexOf("&") + 1, tweetString.length);
  sendToServer(tweetString);
}

let beginreTweeting = function(message) {
  var retweetString = "ReTweet&" + message
  sendToServer(retweetString);
}

let beginSubscription = function() {
  var subscriberString = "Subscribe&" + document.getElementById("subscribe").value
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
  var hashtagString = "QueryHash&" + document.getElementById("hashtagqueryinput").value;
  divMsg = "All tweets with the hashtag you searched for";
  sendToServer(hashtagString);
}

let funcForMessaging = function(evt) {
  var divString = String(evt.data);
  var isSubscriber = divString.startsWith("Your Subscriber");
  if (isSubscriber) {
    var twt = divString.substring(divString.indexOf(":") + 2, divString.length);
    displayDivFunctionForRetweet('<span class="feedcls textfontcls" style="color: black;">' + evt.data + '</span>', twt);
  } else
    displayDivFunction('<span class="feedcls textfontcls" style="color: black;">' + evt.data + '</span>');
}

let displayDivFunction = function(message) {
  var paraElement = document.createElement("div");
  paraElement.style.wordWrap = "break-word";
  var dateSpan = document.createElement('span')
  var dateDiv = new Date().toLocaleTimeString();
  dateSpan.className = "timeCls1";
  dateSpan.innerHTML = dateDiv;
  paraElement.appendChild(dateSpan);
  paraElement.innerHTML = message;
  displayDiv.appendChild(paraElement);

}

let displayDivFunctionForRetweet = function(message, twt) {
  var paraElement = document.createElement("p");
  var btn = document.createElement("BUTTON");
  btn.className = "agbuttonenable";
  btn.innerHTML = "Retweet";
  paraElement.style.wordWrap = "break-word";
  paraElement.innerHTML = message;
  displayDiv.appendChild(paraElement);
  displayDiv.insertAdjacentElement("beforeend", btn);
  btn.addEventListener('click', function () {
    beginreTweeting(twt);
  }, false);
}

let displayErrorMsg = function(evt) {
  displayDivFunction('<span class="feedcls textfontcls" style="color: red;">ERROR:</span> ' + evt.data);
}

let sendToServer = function(message) {
  websocket.send(message);
}

let onOpenFunc = function(evt) { }

bindFunction();
webSocketFunc();