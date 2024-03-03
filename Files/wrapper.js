async function Update() {
    HideError();
    var file = document.getElementById('update-file').files[0];
    var form = new FormData();
    form.append('file', file);

    var request = new XMLHttpRequest();
    request.open('POST', '[PATH_PREFIX]/wrapper/update');
    request.upload.addEventListener('progress', event => {
        document.querySelector('#updateButton').innerText = ((event.loaded / event.total) * 100).toFixed(2) + '%';
    });
    request.onreadystatechange = () => {
        if (request.readyState == 4) {
            switch (request.status) {
                case 200:
                    document.querySelector('#updateButton').innerText = 'Done!';
                    statusEvent.close();
                    statusElem.innerHTML = "<h2>Server updated!</h2><p>Please refresh the page.</p>";
                    statusElem.className = "elem red";
                    break;
                case 418:
                    document.querySelector('#updateButton').innerText = 'Update';
                    ShowError("Invalid file!");
                    break;
                case 503:
                    document.querySelector('#updateButton').innerText = 'Update';
                    ShowError("This server can't be updated like this!");
                    break;
                default:
                    document.querySelector('#updateButton').innerText = 'Update';
                    ShowError("Error!");
                    break;
            }
        }
    };
    request.send(form);
}

async function Revert() {
    if ((await fetch("/api[PATH_PREFIX]/wrapper/revert")).status !== 200) {
        ShowError("Connection failed.");
        return;
    }

    statusEvent.close();
    statusElem.innerHTML = "<h2>Reverted to backed up version!</h2><p>Please refresh the page.</p>";
    statusElem.className = "elem red";
}

async function Restart() {
    if ((await fetch("/api[PATH_PREFIX]/wrapper/restart")).status !== 200) {
        ShowError("Connection failed.");
        return;
    }

    statusEvent.close();
    statusElem.innerHTML = "<h2>Program restarted!</h2><p>Please refresh the page.</p>";
    statusElem.className = "elem red";
}