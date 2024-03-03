async function Work() {
    try {
        if ((await fetch("/api[PATH_PREFIX]/work")).status === 200)
            return;
    } catch { }
    ShowError("Connection failed.");
}