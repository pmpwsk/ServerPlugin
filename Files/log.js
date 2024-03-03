async function Clear() {
    await fetch('/api[PATH_PREFIX]/wrapper/clear-log');
    window.location.reload();
}