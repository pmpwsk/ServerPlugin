async function Clear() {
    let response = await fetch('/api[PATH_PREFIX]/clear-log');
    await response.text();
    window.location.reload();
}