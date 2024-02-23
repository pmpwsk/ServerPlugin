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

async function Restore(id) {
    var btn = document.querySelector("#restore-" + id);
    if (btn.innerText === "Restore...")
        return;
    if (btn.innerText !== "Restore?") {
        btn.innerText = "Restore?";
        return;
    }
    btn.innerText = "Restore...";
    try {
        let response = await fetch("/api[PATH_PREFIX]/backups/restore?id=" + id);
        if (response.status === 200) {
            window.location.reload();
            return;
        }
    } catch {
    }
    btn.innerText = "Restore :(";
    ShowError("Connection failed.");
}