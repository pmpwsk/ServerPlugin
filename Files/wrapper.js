async function Update() {
    HideError();
    var file = document.getElementById("update-file").files[0];
    var form = new FormData();
    form.append("file", file);

    var request = new XMLHttpRequest();
    request.open("POST", "wrapper/update");
    request.upload.addEventListener("progress", event => {
        document.querySelector("#updateButton").innerText = ((event.loaded / event.total) * 100).toFixed(2) + "%";
    });
    request.onreadystatechange = () => {
        if (request.readyState == 4) {
            switch (request.status) {
                case 200:
                    document.querySelector("#updateButton").innerText = "Done!";
                    statusEvent.close();
                    statusElem.innerHTML = "<h2>Server updated!</h2><p>Please <a href=\"javascript:\" onclick=\"window.location.reload()\">refresh</a> the page.</p>";
                    statusElem.className = "elem green";
                    break;
                case 418:
                    document.querySelector("#updateButton").innerText = "Update";
                    ShowError("Invalid file!");
                    break;
                case 503:
                    document.querySelector("#updateButton").innerText = "Update";
                    ShowError("This server can't be updated like this!");
                    break;
                default:
                    document.querySelector("#updateButton").innerText = "Update";
                    ShowError("Error!");
                    break;
            }
        }
    };
    request.send(form);
}

async function Revert() {
    if ((await fetch("wrapper/revert", {method:"POST"})).status !== 200) {
        ShowError("Connection failed.");
        return;
    }
    statusEvent.close();
    statusElem.innerHTML = "<h2>Reverted to backed up version!</h2><p>Please <a href=\"javascript:\" onclick=\"window.location.reload()\">refresh</a> the page.</p>";
    statusElem.className = "elem green";
}

async function Restart() {
    if ((await fetch("wrapper/restart", {method:"POST"})).status !== 200) {
        ShowError("Connection failed.");
        return;
    }
    statusEvent.close();
    statusElem.innerHTML = "<h2>Program restarted!</h2><p>Please <a href=\"javascript:\" onclick=\"window.location.reload()\">refresh</a> the page.</p>";
    statusElem.className = "elem green";
}

async function Stop() {
    var stopButton = document.querySelector("#stopButton").firstElementChild;
    if (stopButton.innerText !== "Stop program?") {
        stopButton.innerText = "Stop program?";
        return;
    }
    if ((await fetch("wrapper/stop", {method:"POST"})).status !== 200) {
        ShowError("Connection failed.");
        return;
    }
    stopButton.innerText = "Stop program";
    statusEvent.close();
    statusElem.innerHTML = "<h2>Program stopped!</h2><p>The program is shutting down.</p>";
    statusElem.className = "elem green";
}

async function ReloadConfig() {
    if ((await fetch("wrapper/reload-config", {method:"POST"})).status !== 200) {
        ShowError("Connection failed.");
        return;
    }
    statusEvent.close();
    statusElem.innerHTML = "<h2>Wrapper config reloaded!</h2><p>The Wrapper has been told to reload the configuration, the new one will be used on the next restart of the program.</p>";
    statusElem.className = "elem green";
}