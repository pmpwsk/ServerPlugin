async function BackupNow(fresh) {
    var warning = document.querySelector("#backup-running");
    warning.style.removeProperty("display");
    try {
        let response = await fetch("/api[PATH_PREFIX]/backups/new?fresh=" + fresh);
        if (response.status === 200) {
            window.location.reload();
            return;
        }
    } catch {
    }
    warning.style["display"] = "none";
    ShowError("Connection failed.");
}