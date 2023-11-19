async function Update() {
    HideError();
    let file = document.getElementById('update-file').files[0];
    let form = new FormData();
    form.append('file', file);

    let request = new XMLHttpRequest();
    request.open('POST', '[PATH_PREFIX]/update');
    request.upload.addEventListener('progress', event => {
        document.querySelector('#updateButton').firstElementChild.innerText = 'Updating... ' + ((event.loaded / event.total) * 100).toFixed(2) + '%';
    });
    request.onreadystatechange = () => {
        if (request.readyState == 4) {
            switch (request.status) {
                case 200:
                    window.location.reload();
                    break;
                case 418:
                    document.querySelector('#updateButton').firstElementChild.innerText = 'Update';
                    ShowError("Invalid file!");
                    break;
                case 503:
                    document.querySelector('#updateButton').firstElementChild.innerText = 'Update';
                    ShowError("This server can't be updated like this!");
                    break;
                default:
                    document.querySelector('#updateButton').firstElementChild.innerText = 'Update';
                    ShowError("Error!");
                    break;
            }
        }
    };
    request.send(form);
}