let to = document.querySelector("#to");
let from = document.querySelector("#from");
let subject = document.querySelector("#subject");
let text = document.querySelector("#text");

async function Send() {
    if (to.value === "" || from.value === "" || subject.value === "" || text.value === "") {
        ShowError("Fill out everything.");
    } else {
        let response = await fetch("/api[PATH_PREFIX]/send-mail?to=" + encodeURIComponent(to.value) + "&from=" + encodeURIComponent(from.value) + "&subject=" + encodeURIComponent(subject.value) + "&text=" + encodeURIComponent(text.value));
        if (response.status === 200) {
            let text = await response.text();
            ShowError(text);
        } else {
            ShowError("Connection failed.");
        }
    }
}