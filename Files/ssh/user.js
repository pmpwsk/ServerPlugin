let to = document.querySelector("#to");
let from = document.querySelector("#from");
let subject = document.querySelector("#subject");
let text = document.querySelector("#text");

function GetUser() {
    return (new URLSearchParams(window.location.search)).get("username");
}

async function Enable() {
    switch (await SendRequest(`user/enable?username=${GetUser()}`, "POST", true)) {
        case 200: window.location.reload(); break;
        default: ShowError("Connection failed."); break;
    }
}

async function Disable() {
    switch (await SendRequest(`user/disable?username=${GetUser()}`, "POST", true)) {
        case 200: window.location.reload(); break;
        default: ShowError("Connection failed."); break;
    }
}

async function Delete(tbd) {
    switch (await SendRequest(`user/delete?username=${GetUser()}&key=${tbd}`, "POST", true)) {
        case 200: window.location.reload(); break;
        default: ShowError("Connection failed."); break;
    }
}

async function Add() {
    var key = document.querySelector("#key").value;
    if (key === "")
        ShowError("Enter a public key.");
    else 
        switch (await SendRequest(`user/add?username=${GetUser()}&key=${encodeURIComponent(key)}`, "POST", true)) {
            case 200: window.location.reload(); break;
            default: ShowError("Connection failed."); break;
        }
}