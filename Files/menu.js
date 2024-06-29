async function Work() {
    try {
        if ((await fetch("work", {method:"POST"})).status === 200)
            return;
    } catch { }
    ShowError("Connection failed.");
}