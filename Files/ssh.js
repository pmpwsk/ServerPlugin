async function Enable(username) {
    switch (await SendRequest(`ssh/user/enable?username=${username}`, "POST", true)) {
        case 200: window.location.reload(); break;
        default: ShowError("Connection failed."); break;
    }
}

async function Disable(username) {
    switch (await SendRequest(`ssh/user/disable?username=${username}`, "POST", true)) {
        case 200: window.location.reload(); break;
        default: ShowError("Connection failed."); break;
    }
}

async function Block() {
    switch (await SendRequest("ssh/block", "POST", true)) {
        case 200: window.location.reload(); break;
        default: ShowError("Connection failed."); break;
    }
}

async function Change() {
    switch (await SendRequest("ssh/change", "POST", true)) {
        case 200: window.location.reload(); break;
        default: ShowError("Connection failed."); break;
    }
}

async function Allow() {
    switch (await SendRequest("ssh/allow", "POST", true)) {
        case 200: window.location.reload(); break;
        default: ShowError("Connection failed."); break;
    }
}