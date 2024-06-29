async function BackupNow(fresh) {
    try {
        if ((await fetch(`backups/new?fresh=${fresh}`, {method:"POST"})).status === 200) {
            window.location.reload();
            return;
        }
    } catch {
    }
    ShowError("Connection failed.");
}

async function Restore(id) {
    var btn = document.querySelector(`#restore-${id}`);
    if (btn.innerText === "Restore...")
        return;
    if (btn.innerText !== "Restore?") {
        btn.innerText = "Restore?";
        return;
    }
    btn.innerText = "Restore...";
    try {
        if ((await fetch(`backups/restore?id=${id}`, {method:"POST"})).status === 200) {
            window.location.reload();
            return;
        }
    } catch {
    }
    btn.innerText = "Restore :(";
    ShowError("Connection failed.");
}