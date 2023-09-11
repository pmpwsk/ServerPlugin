let to = document.querySelector("#to");
let from = document.querySelector("#from");
let subject = document.querySelector("#subject");
let text = document.querySelector("#text");

function GetUser() {
    let query = new URLSearchParams(window.location.search);
    return query.get("user");
}

async function Enable() {
    let response = await fetch("/api[PATH_PREFIX]/ssh/enable?user=" + GetUser());
    if (response.status === 200) {
        let text = await response.text();
        if (text === "ok") {
            window.location.reload();
        } else {
            ShowError("Connection failed.");
        }
    } else {
        ShowError("Connection failed.");
    }
}

async function Disable() {
    let response = await fetch("/api[PATH_PREFIX]/ssh/disable?user=" + GetUser());
    if (response.status === 200) {
        let text = await response.text();
        if (text === "ok") {
            window.location.reload();
        } else {
            ShowError("Connection failed.");
        }
    } else {
        ShowError("Connection failed.");
    }
}

async function Delete(tbd) {
    let response = await fetch("/api[PATH_PREFIX]/ssh/delete?user=" + GetUser() + "&pk=" + tbd);
    if (response.status === 200) {
        let text = await response.text();
        if (text === "ok") {
            window.location.reload();
        } else {
            ShowError("Connection failed.");
        }
    } else {
        ShowError("Connection failed.");
    }
}

async function Add() {
    let pk = document.querySelector("#pk");
    if (pk.value === "") {
        ShowError("Enter a public key.");
    } else {
        let response = await fetch("/api[PATH_PREFIX]/ssh/add?user=" + GetUser() + "&pk=" + encodeURIComponent(pk.value));
        if (response.status === 200) {
            let text = await response.text();
            if (text === "ok") {
                window.location.reload();
            } else {
                ShowError("Connection failed.");
            }
        } else {
            ShowError("Connection failed.");
        }
    }
}