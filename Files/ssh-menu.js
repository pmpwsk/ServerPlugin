async function Disable(user) {
    let response = await fetch('/api[PATH_PREFIX]/ssh/disable?user=' + user);
    await response.text();
    window.location.reload();
}

async function Enable(user) {
    let response = await fetch('/api[PATH_PREFIX]/ssh/enable?user=' + user);
    await response.text();
    window.location.reload();
}

async function Block() {
    let response = await fetch('/api[PATH_PREFIX]/ssh/block');
    await response.text();
    window.location.reload();
}

async function Change() {
    let response = await fetch('/api[PATH_PREFIX]/ssh/change');
    await response.text();
    window.location.reload();
}

async function Allow() {
    let response = await fetch('/api[PATH_PREFIX]/ssh/allow');
    await response.text();
    window.location.reload();
}