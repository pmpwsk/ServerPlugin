async function Restart() {
    try {
        var response = await fetch("mail/restart", {method:"POST"});
        switch (response.status) {
            case 200:
                statusEvent.close();
                statusElem.innerHTML = "<h2>Mail server restarted!</h2>";
                statusElem.className = "elem green";
                return;
            case 500:
                statusEvent.close();
                statusElem.innerHTML = `<h2>Failed to restart the mail server!</h2><p>${await response.text()}</p>`;
                statusElem.className = "elem red";
                return;
        }
    } catch {
    }
    statusEvent.close();
    statusElem.innerHTML = `<h2>Failed to restart the mail server!</h2><p>Connection failed.</p>`;
    statusElem.className = "elem red";
}