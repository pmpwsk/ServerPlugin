async function Clear() {
    await fetch("clear-log", {method:"POST"});
    window.location.reload();
}